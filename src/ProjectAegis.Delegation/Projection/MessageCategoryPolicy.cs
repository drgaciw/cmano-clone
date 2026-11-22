namespace ProjectAegis.Delegation.Projection;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using ProjectAegis.Data.Scenario;

[Flags]
public enum MessageNotificationActions
{
    None = 0,
    Log = 1 << 0,
    Pause = 1 << 1,
    MapBalloon = 1 << 2,
    DropToRealTime = 1 << 3,
}

/// <summary>Per-category notification actions (DRG-408).</summary>
public readonly record struct MessageCategoryRule(
    bool Log,
    bool Pause,
    bool MapBalloon,
    bool DropToRealTime)
{
    public static MessageCategoryRule DefaultLogOnly { get; } = new(true, false, false, false);

    public MessageNotificationActions ToActions()
    {
        var actions = MessageNotificationActions.None;
        if (Log) actions |= MessageNotificationActions.Log;
        if (Pause) actions |= MessageNotificationActions.Pause;
        if (MapBalloon) actions |= MessageNotificationActions.MapBalloon;
        if (DropToRealTime) actions |= MessageNotificationActions.DropToRealTime;
        return actions;
    }

    public string FormatActionTokens()
    {
        var parts = new List<string>(4);
        if (Log) parts.Add("LOG");
        if (Pause) parts.Add("PAUSE");
        if (MapBalloon) parts.Add("BALLOON");
        if (DropToRealTime) parts.Add("1X");
        return parts.Count > 0 ? "[" + string.Join("|", parts) + "]" : "[MUTED]";
    }
}

/// <summary>
/// Data-driven per-category message-log notification policy (DRG-408).
/// Immutable once constructed; <see cref="WithRule"/> returns a new instance.
/// </summary>
public sealed class MessageCategoryPolicy
{
    public const string DefaultFileName = "message-category-policy.default.json";
    public const string EmbeddedResourceName = "ProjectAegis.MessageCategoryPolicyDefault.json";

    private readonly Dictionary<string, MessageCategoryRule> _rules;
    private readonly string[] _categories;

    public MessageCategoryPolicy(
        IReadOnlyDictionary<string, MessageCategoryRule> rules,
        MessageCategoryRule? fallbackRule = null)
    {
        if (rules is null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        FallbackRule = fallbackRule ?? MessageCategoryRule.DefaultLogOnly;
        _rules = new Dictionary<string, MessageCategoryRule>(StringComparer.Ordinal);
        foreach (var pair in rules)
        {
            var normalized = NormalizeCategory(pair.Key);
            if (!string.IsNullOrEmpty(normalized))
            {
                _rules[normalized] = pair.Value;
            }
        }

        _categories = new string[_rules.Count];
        _rules.Keys.CopyTo(_categories, 0);
        Array.Sort(_categories, StringComparer.Ordinal);
    }

    public IReadOnlyList<string> Categories => _categories;

    public MessageCategoryRule FallbackRule { get; }

    public MessageCategoryRule GetRule(string? category)
    {
        var normalized = NormalizeCategory(category);
        if (string.IsNullOrEmpty(normalized))
        {
            return FallbackRule;
        }

        return _rules.TryGetValue(normalized, out var rule) ? rule : FallbackRule;
    }

    public MessageCategoryPolicy WithRule(string category, MessageCategoryRule rule)
    {
        var normalized = NormalizeCategory(category);
        if (string.IsNullOrEmpty(normalized))
        {
            throw new ArgumentException("Category must be non-empty.", nameof(category));
        }

        var updated = new Dictionary<string, MessageCategoryRule>(_rules, StringComparer.Ordinal)
        {
            [normalized] = rule,
        };
        return new MessageCategoryPolicy(updated, FallbackRule);
    }

    public static MessageCategoryPolicy LoadDefault()
    {
        var scenariosDir = ScenarioDataPaths.TryResolveScenariosDirectory();
        if (!string.IsNullOrEmpty(scenariosDir))
        {
            var diskPath = Path.Combine(scenariosDir, DefaultFileName);
            if (File.Exists(diskPath))
            {
                return FromJson(File.ReadAllText(diskPath, Encoding.UTF8));
            }
        }

        var assembly = typeof(MessageCategoryPolicy).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Missing embedded notification policy resource: {EmbeddedResourceName}");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return FromJson(reader.ReadToEnd());
    }

    public static MessageCategoryPolicy FromJson(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new FormatException("Message category policy JSON root must be an object.");
        }

        var fallback = MessageCategoryRule.DefaultLogOnly;
        if (root.TryGetProperty("fallbackRule", out var fallbackElement))
        {
            fallback = ParseRule(fallbackElement, MessageCategoryRule.DefaultLogOnly);
        }

        var rules = new Dictionary<string, MessageCategoryRule>(StringComparer.Ordinal);
        if (root.TryGetProperty("categories", out var categoriesElement)
            && categoriesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in categoriesElement.EnumerateObject())
            {
                var key = NormalizeCategory(prop.Name);
                if (!string.IsNullOrEmpty(key))
                {
                    rules[key] = ParseRule(prop.Value, fallback);
                }
            }
        }

        return new MessageCategoryPolicy(rules, fallback);
    }

    public string ToJson()
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", 1);

            writer.WritePropertyName("fallbackRule");
            WriteRule(writer, FallbackRule);

            writer.WriteStartObject("categories");
            for (var i = 0; i < _categories.Length; i++)
            {
                var key = _categories[i];
                writer.WritePropertyName(key);
                WriteRule(writer, _rules[key]);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static string NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? string.Empty : category.Trim().ToUpperInvariant();

    private static MessageCategoryRule ParseRule(JsonElement element, MessageCategoryRule defaults)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return defaults;
        }

        var log = TryGetBool(element, "log", defaults.Log);
        var pause = TryGetBool(element, "pause", defaults.Pause);
        var mapBalloon = TryGetBool(element, "mapBalloon", defaults.MapBalloon);
        var dropToRealTime = TryGetBool(element, "dropToRealTime", defaults.DropToRealTime);
        return new MessageCategoryRule(log, pause, mapBalloon, dropToRealTime);
    }

    private static bool TryGetBool(JsonElement obj, string name, bool fallback) =>
        obj.TryGetProperty(name, out var prop) && prop.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? prop.GetBoolean()
            : fallback;

    private static void WriteRule(Utf8JsonWriter writer, MessageCategoryRule rule)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("log", rule.Log);
        writer.WriteBoolean("pause", rule.Pause);
        writer.WriteBoolean("mapBalloon", rule.MapBalloon);
        writer.WriteBoolean("dropToRealTime", rule.DropToRealTime);
        writer.WriteEndObject();
    }
}
