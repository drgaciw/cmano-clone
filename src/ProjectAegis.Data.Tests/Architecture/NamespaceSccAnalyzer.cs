namespace ProjectAegis.Data.Tests.Architecture;

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

/// <summary>
/// Compiler-exact (PE metadata) intra-assembly namespace dependency SCCs.
/// Edges come from type signatures only (base, interfaces, fields, methods, properties) —
/// not source heuristics or regex graphs. DRG-771 Lane C / CMA-30.
/// </summary>
internal static class NamespaceSccAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> LoadBaseline(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var raw = JsonSerializer.Deserialize<Dictionary<string, List<List<string>>>>(json, JsonOptions)
                  ?? new Dictionary<string, List<List<string>>>();
        return raw.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<IReadOnlyList<string>>)kv.Value
                .Select(scc => (IReadOnlyList<string>)scc.ToArray())
                .ToArray(),
            StringComparer.Ordinal);
    }

    public static IReadOnlyList<IReadOnlyList<string>> AnalyzeAssembly(string dllPath)
    {
        using var stream = File.OpenRead(dllPath);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var graph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var assemblyNs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var th in reader.TypeDefinitions)
        {
            var td = reader.GetTypeDefinition(th);
            var ns = reader.GetString(td.Namespace);
            if (string.IsNullOrEmpty(ns) || !ns.StartsWith("ProjectAegis.", StringComparison.Ordinal))
            {
                continue;
            }

            assemblyNs.Add(ns);
            if (!graph.ContainsKey(ns))
            {
                graph[ns] = new HashSet<string>(StringComparer.Ordinal);
            }

            AddEdgesFromTypeHandle(graph, ns, reader, td.BaseType);

            foreach (var iface in td.GetInterfaceImplementations())
            {
                var impl = reader.GetInterfaceImplementation(iface);
                AddEdgesFromTypeHandle(graph, ns, reader, impl.Interface);
            }

            foreach (var fh in td.GetFields())
            {
                var field = reader.GetFieldDefinition(fh);
                var blob = reader.GetBlobReader(field.Signature);
                blob.ReadByte();
                AddEdgesFromTypeSig(graph, ns, reader, ref blob);
            }

            foreach (var mh in td.GetMethods())
            {
                var method = reader.GetMethodDefinition(mh);
                var blob = reader.GetBlobReader(method.Signature);
                var header = blob.ReadSignatureHeader();
                if (header.IsGeneric)
                {
                    blob.ReadCompressedInteger();
                }

                var paramCount = blob.ReadCompressedInteger();
                AddEdgesFromTypeSig(graph, ns, reader, ref blob);
                for (var i = 0; i < paramCount; i++)
                {
                    AddEdgesFromTypeSig(graph, ns, reader, ref blob);
                }
            }

            foreach (var ph in td.GetProperties())
            {
                var prop = reader.GetPropertyDefinition(ph);
                var blob = reader.GetBlobReader(prop.Signature);
                blob.ReadSignatureHeader();
                var paramCount = blob.ReadCompressedInteger();
                AddEdgesFromTypeSig(graph, ns, reader, ref blob);
                for (var i = 0; i < paramCount; i++)
                {
                    AddEdgesFromTypeSig(graph, ns, reader, ref blob);
                }
            }
        }

        var filtered = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var (from, tos) in graph)
        {
            if (!assemblyNs.Contains(from))
            {
                continue;
            }

            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var to in tos)
            {
                if (assemblyNs.Contains(to))
                {
                    set.Add(to);
                }
            }

            filtered[from] = set;
        }

        return StronglyConnectedComponents(filtered);
    }

    /// <summary>
    /// Returns multi-node SCCs present in <paramref name="actual"/> that are not covered by
    /// any baseline SCC (new cycles or namespaces newly joining a cycle).
    /// Shrinking SCCs (breaking cycles) is allowed and does not fail.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> FindNewCycles(
        IReadOnlyList<IReadOnlyList<string>> baseline,
        IReadOnlyList<IReadOnlyList<string>> actual)
    {
        var baselineKeys = new HashSet<string>(
            baseline.Select(CanonicalKey),
            StringComparer.Ordinal);

        // Also allow actual SCCs that are subsets of a baseline SCC (cycle shrunk).
        var baselineSets = baseline
            .Select(scc => new HashSet<string>(scc, StringComparer.Ordinal))
            .ToList();

        var novel = new List<IReadOnlyList<string>>();
        foreach (var scc in actual)
        {
            var key = CanonicalKey(scc);
            if (baselineKeys.Contains(key))
            {
                continue;
            }

            var set = new HashSet<string>(scc, StringComparer.Ordinal);
            var subsetOfBaseline = baselineSets.Any(b => set.IsSubsetOf(b));
            if (subsetOfBaseline)
            {
                continue;
            }

            novel.Add(scc);
        }

        return novel;
    }

    public static string CanonicalKey(IEnumerable<string> scc) =>
        string.Join("|", scc.OrderBy(s => s, StringComparer.Ordinal));

    private static string? ResolveNs(MetadataReader reader, EntityHandle handle)
    {
        if (handle.IsNil)
        {
            return null;
        }

        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
            {
                var td = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                var ns = reader.GetString(td.Namespace);
                return string.IsNullOrEmpty(ns) ? null : ns;
            }
            case HandleKind.TypeReference:
            {
                var tr = reader.GetTypeReference((TypeReferenceHandle)handle);
                var ns = reader.GetString(tr.Namespace);
                return string.IsNullOrEmpty(ns) ? null : ns;
            }
            case HandleKind.TypeSpecification:
            {
                // Single-ns ResolveNs is only used for base/iface handles; TypeSpecs with
                // generic args are decoded via AddEdgesFromTypeSig at signature sites.
                var ts = reader.GetTypeSpecification((TypeSpecificationHandle)handle);
                var blob = reader.GetBlobReader(ts.Signature);
                var collected = new HashSet<string>(StringComparer.Ordinal);
                CollectTypeSigNamespaces(reader, ref blob, collected);
                return collected.FirstOrDefault();
            }
            default:
                return null;
        }
    }

    private static void AddEdgesFromTypeHandle(
        Dictionary<string, HashSet<string>> graph,
        string from,
        MetadataReader reader,
        EntityHandle handle)
    {
        if (handle.IsNil)
        {
            return;
        }

        if (handle.Kind == HandleKind.TypeSpecification)
        {
            var ts = reader.GetTypeSpecification((TypeSpecificationHandle)handle);
            var blob = reader.GetBlobReader(ts.Signature);
            AddEdgesFromTypeSig(graph, from, reader, ref blob);
            return;
        }

        AddEdge(graph, from, ResolveNs(reader, handle));
    }

    private static void AddEdgesFromTypeSig(
        Dictionary<string, HashSet<string>> graph,
        string from,
        MetadataReader reader,
        ref BlobReader blob)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        CollectTypeSigNamespaces(reader, ref blob, namespaces);
        foreach (var ns in namespaces)
        {
            AddEdge(graph, from, ns);
        }
    }

    /// <summary>
    /// Collects every namespace referenced by a type signature, including constructed
    /// generic type arguments (e.g. <c>IReadOnlyList&lt;ValidationFinding&gt;</c> yields both
    /// the collection type ns and <c>ValidationFinding</c>'s ns).
    /// </summary>
    private static void CollectTypeSigNamespaces(
        MetadataReader reader,
        ref BlobReader blob,
        HashSet<string> sink)
    {
        var code = blob.ReadByte();
        switch ((SignatureTypeCode)code)
        {
            case SignatureTypeCode.GenericTypeInstance:
            {
                blob.ReadByte();
                var typeHandle = blob.ReadTypeHandle();
                var typeNs = ResolveNs(reader, typeHandle);
                if (!string.IsNullOrEmpty(typeNs))
                {
                    sink.Add(typeNs);
                }

                var argCount = blob.ReadCompressedInteger();
                for (var i = 0; i < argCount; i++)
                {
                    CollectTypeSigNamespaces(reader, ref blob, sink);
                }

                return;
            }
            case SignatureTypeCode.SZArray:
            case SignatureTypeCode.Pointer:
            case SignatureTypeCode.ByReference:
                CollectTypeSigNamespaces(reader, ref blob, sink);
                return;
            case SignatureTypeCode.Array:
            {
                CollectTypeSigNamespaces(reader, ref blob, sink);
                blob.ReadCompressedInteger(); // rank
                var sizes = blob.ReadCompressedInteger();
                for (var i = 0; i < sizes; i++)
                {
                    blob.ReadCompressedInteger();
                }

                var loBounds = blob.ReadCompressedInteger();
                for (var i = 0; i < loBounds; i++)
                {
                    blob.ReadCompressedSignedInteger();
                }

                return;
            }
            case SignatureTypeCode.GenericTypeParameter:
            case SignatureTypeCode.GenericMethodParameter:
                blob.ReadCompressedInteger();
                return;
            default:
                if (code is 0x11 or 0x12)
                {
                    var typeHandle = blob.ReadTypeHandle();
                    var ns = ResolveNs(reader, typeHandle);
                    if (!string.IsNullOrEmpty(ns))
                    {
                        sink.Add(ns);
                    }
                }

                return;
        }
    }

    private static void AddEdge(Dictionary<string, HashSet<string>> graph, string from, string? to)
    {
        if (string.IsNullOrEmpty(to) || from == to)
        {
            return;
        }

        if (!from.StartsWith("ProjectAegis.", StringComparison.Ordinal)
            || !to.StartsWith("ProjectAegis.", StringComparison.Ordinal))
        {
            return;
        }

        if (!graph.TryGetValue(from, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            graph[from] = set;
        }

        set.Add(to);
        if (!graph.ContainsKey(to))
        {
            graph[to] = new HashSet<string>(StringComparer.Ordinal);
        }
    }

    private static List<IReadOnlyList<string>> StronglyConnectedComponents(
        Dictionary<string, HashSet<string>> graph)
    {
        var index = 0;
        var stack = new Stack<string>();
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowlink = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<IReadOnlyList<string>>();

        void StrongConnect(string v)
        {
            indices[v] = index;
            lowlink[v] = index;
            index++;
            stack.Push(v);
            onStack.Add(v);

            foreach (var w in graph.GetValueOrDefault(v) ?? Enumerable.Empty<string>())
            {
                if (!indices.ContainsKey(w))
                {
                    StrongConnect(w);
                    lowlink[v] = Math.Min(lowlink[v], lowlink[w]);
                }
                else if (onStack.Contains(w))
                {
                    lowlink[v] = Math.Min(lowlink[v], indices[w]);
                }
            }

            if (lowlink[v] == indices[v])
            {
                var comp = new List<string>();
                string w;
                do
                {
                    w = stack.Pop();
                    onStack.Remove(w);
                    comp.Add(w);
                }
                while (!StringComparer.Ordinal.Equals(w, v));

                if (comp.Count > 1)
                {
                    comp.Sort(StringComparer.Ordinal);
                    result.Add(comp);
                }
            }
        }

        foreach (var v in graph.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!indices.ContainsKey(v))
            {
                StrongConnect(v);
            }
        }

        result.Sort((a, b) =>
            StringComparer.Ordinal.Compare(CanonicalKey(a), CanonicalKey(b)));
        return result;
    }
}
