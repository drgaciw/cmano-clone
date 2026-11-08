namespace ProjectAegis.Data.Validation;

using System.Reflection;

/// <summary>AC-9: editorState is derived-only; sim/validation must not read it.</summary>
public static class EditorStateSchemaLint
{
    private static readonly string[] ForbiddenConsumerAssemblies =
    [
        "ProjectAegis.Sim",
        "ProjectAegis.Data",
    ];

    public static IReadOnlyList<string> FindViolations()
    {
        var violations = new List<string>();
        foreach (var assemblyName in ForbiddenConsumerAssemblies)
        {
            Assembly? assembly;
            try
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));
            }
            catch
            {
                continue;
            }

            if (assembly == null)
            {
                continue;
            }

            foreach (var type in assembly.GetTypes())
            {
                if (type.Namespace?.Contains("Validation", StringComparison.Ordinal) != true &&
                    type.Namespace?.Contains("Sim", StringComparison.Ordinal) != true)
                {
                    continue;
                }

                foreach (var member in type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (member.Name.Contains("EditorState", StringComparison.Ordinal) &&
                        member.DeclaringType?.Name != nameof(Scenario.Authoring.ScenarioEditorStateDto) &&
                        !member.DeclaringType?.Name.Contains("Lint", StringComparison.Ordinal) == true &&
                        !member.DeclaringType?.Name.Contains("StableJson", StringComparison.Ordinal) == true &&
                        !member.DeclaringType?.Name.Contains("Package", StringComparison.Ordinal) == true)
                    {
                        violations.Add($"{assemblyName}:{type.FullName}.{member.Name}");
                    }
                }
            }
        }

        return violations;
    }
}
