namespace ProjectAegis.Delegation.Hindsight;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class HindsightHttpMemoryClient : IHindsightMemoryClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly string? _apiKey;

    public HindsightHttpMemoryClient(HttpClient http, string? apiKey = null)
    {
        _http = http;
        _apiKey = apiKey;
    }

    public void RetainFireAndForget(string bankId, string content, string? context = null) =>
        _ = Task.Run(async () =>
        {
            try
            {
                await RetainAsync(bankId, content, context, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Hindsight is optional infrastructure; failures must not affect simulation.
            }
        });

    public Task RetainAsync(string bankId, string content, CancellationToken cancellationToken = default) =>
        RetainAsync(bankId, content, context: null, cancellationToken);

    public async Task RetainAsync(
        string bankId,
        string content,
        string? context,
        CancellationToken cancellationToken)
    {
        var path = $"/v1/default/banks/{Uri.EscapeDataString(bankId)}/memories/retain";
        var body = new RetainRequestDto(
            [new RetainItemDto(content, context)],
            Async: true);

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        ApplyAuth(request);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> ReflectAsync(
        string bankId,
        string query,
        CancellationToken cancellationToken = default)
    {
        var path = $"/v1/default/banks/{Uri.EscapeDataString(bankId)}/reflect";
        var body = new ReflectRequestDto(query, Budget: "mid", IncludeFacts: true);

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        ApplyAuth(request);

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var parsed = await JsonSerializer.DeserializeAsync<ReflectResponseDto>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return parsed?.Text;
    }

    public void Dispose() => _http.Dispose();

    private void ApplyAuth(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    private sealed record RetainRequestDto(RetainItemDto[] Items, bool Async);

    private sealed record RetainItemDto(string Content, string? Context);

    private sealed record ReflectRequestDto(string Query, string Budget, bool IncludeFacts);

    private sealed record ReflectResponseDto(string? Text);
}
