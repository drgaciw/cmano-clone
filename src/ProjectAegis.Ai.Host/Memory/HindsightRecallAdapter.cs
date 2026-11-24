using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Memory;

/// <summary>
/// HTTP recall adapter calling <c>POST /v1/default/banks/{bankId}/memories/recall</c>
/// (DRG-746 / AI-S2-4). Maps unreachable, HTTP error, timeout, cancellation, and malformed
/// JSON responses to explicit <see cref="GenerationStatus"/> values without throwing.
/// </summary>
public sealed class HindsightRecallAdapter : IHindsightRecallClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly string? _apiKey;

    /// <summary>Initializes a new <see cref="HindsightRecallAdapter"/> using the supplied <see cref="HttpClient"/>.</summary>
    public HindsightRecallAdapter(HttpClient httpClient, string? apiKey = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey;
    }

    /// <inheritdoc />
    public async Task<HindsightRecallResult> RecallAsync(
        HindsightRecallRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BankId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var budget = string.IsNullOrWhiteSpace(request.Budget) ? "mid" : request.Budget;
        var maxTokens = request.MaxTokens > 0 ? request.MaxTokens : 4096;
        var path = $"/v1/default/banks/{Uri.EscapeDataString(request.BankId)}/memories/recall";
        var body = new { query = request.Query, budget, maxTokens };

        using var message = new HttpRequestMessage(HttpMethod.Post, path);
        message.Content = JsonContent.Create(body, options: JsonOptions);
        if (!string.IsNullOrEmpty(_apiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            return Fail(GenerationStatus.Cancelled, request, ex.Message);
        }
        catch (OperationCanceledException ex)
        {
            return Fail(GenerationStatus.TimedOut, request, ex.Message);
        }
        catch (TimeoutException ex)
        {
            return Fail(GenerationStatus.TimedOut, request, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return Fail(GenerationStatus.ProviderUnavailable, request, ex.Message);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var status = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout
                    ? GenerationStatus.TimedOut
                    : GenerationStatus.ProviderUnavailable;
                return Fail(status, request, $"HTTP {(int)response.StatusCode} ({response.StatusCode})");
            }

            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var dto = await JsonSerializer.DeserializeAsync<RecallResponseDto>(stream, JsonOptions, cancellationToken)
                    .ConfigureAwait(false);
                if (dto?.Results is null)
                {
                    return Fail(GenerationStatus.MalformedOutput, request, "Recall response missing 'results' array.");
                }

                var items = new List<HindsightRecallItem>(dto.Results.Count);
                for (var i = 0; i < dto.Results.Count; i++)
                {
                    var row = dto.Results[i];
                    if (row is null || row.Text is null)
                    {
                        return Fail(GenerationStatus.MalformedOutput, request, "Recall result item missing 'text'.");
                    }

                    items.Add(new HindsightRecallItem(
                        Text: row.Text,
                        Score: row.Score,
                        Id: row.Metadata?.Id,
                        BankId: row.Metadata?.BankId,
                        Context: row.Metadata?.Context,
                        CreatedAt: row.Metadata?.CreatedAt));
                }

                return new HindsightRecallResult(
                    GenerationStatus.Succeeded,
                    dto.BankId ?? request.BankId,
                    dto.Query ?? request.Query,
                    items);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                return Fail(GenerationStatus.Cancelled, request, ex.Message);
            }
            catch (OperationCanceledException ex)
            {
                return Fail(GenerationStatus.TimedOut, request, ex.Message);
            }
            catch (JsonException ex)
            {
                return Fail(GenerationStatus.MalformedOutput, request, ex.Message);
            }
        }
    }

    private static HindsightRecallResult Fail(
        GenerationStatus status,
        HindsightRecallRequest request,
        string error) =>
        new(
            status,
            request.BankId,
            request.Query,
            Array.Empty<HindsightRecallItem>(),
            error);

    private sealed record RecallResponseDto(
        string? BankId,
        string? Query,
        List<RecallResultItemDto?>? Results);

    private sealed record RecallResultItemDto(
        string? Text,
        double Score,
        RecallMetadataDto? Metadata);

    private sealed record RecallMetadataDto(
        string? Id,
        string? BankId,
        string? Context,
        string? CreatedAt);
}
