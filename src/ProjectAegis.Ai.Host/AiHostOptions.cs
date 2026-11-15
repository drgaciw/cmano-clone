namespace ProjectAegis.Ai.Host;

/// <summary>Host limits for Wave 0 AI composition (CI-safe defaults).</summary>
public sealed class AiHostOptions
{
    /// <summary>Maximum concurrent chat requests for this host instance.</summary>
    public int MaxRequestConcurrency { get; set; } = 4;

    /// <summary>Maximum published evidence document size in bytes.</summary>
    public int MaxEvidenceBytes { get; set; } = 256 * 1024;

    /// <summary>Soft output-token budget applied to <see cref="Microsoft.Extensions.AI.ChatOptions.MaxOutputTokens"/>.</summary>
    public int MaxOutputTokens { get; set; } = 1024;

    /// <summary>Per-request timeout.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
