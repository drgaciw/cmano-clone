using System.Net;
using System.Text.Json;
using NUnit.Framework;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Memory;

namespace ProjectAegis.Ai.Host.Tests.Memory;

[TestFixture]
public sealed class HindsightRecallAdapterTests
{
    [Test]
    public async Task RecallAsync_posts_to_memories_recall_and_parses_results()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(async (request, _) =>
        {
            captured = request;
            capturedBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "bankId": "dev-cmano-clone",
                  "query": "baltic roe",
                  "results": [
                    {
                      "text": "Keep WeaponsTight until contact classified",
                      "score": 4.0,
                      "metadata": {
                        "id": "mem-1",
                        "bankId": "dev-cmano-clone",
                        "context": "aar",
                        "createdAt": "2026-11-22T00:00:00Z"
                      }
                    }
                  ]
                }
                """),
            };
        });

        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("http://localhost:8888/");
        IHindsightRecallClient adapter = new HindsightRecallAdapter(http, apiKey: "secret-key");

        var result = await adapter.RecallAsync(new HindsightRecallRequest("dev-cmano-clone", "baltic roe", "high", 2048));

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(
            captured.RequestUri!.ToString(),
            Is.EqualTo("http://localhost:8888/v1/default/banks/dev-cmano-clone/memories/recall"));
        Assert.That(captured.Headers.Authorization?.Scheme, Is.EqualTo("Bearer"));
        Assert.That(captured.Headers.Authorization?.Parameter, Is.EqualTo("secret-key"));

        using var doc = JsonDocument.Parse(capturedBody!);
        Assert.That(doc.RootElement.GetProperty("query").GetString(), Is.EqualTo("baltic roe"));
        Assert.That(doc.RootElement.GetProperty("budget").GetString(), Is.EqualTo("high"));
        Assert.That(doc.RootElement.GetProperty("maxTokens").GetInt32(), Is.EqualTo(2048));

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.Succeeded));
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.BankId, Is.EqualTo("dev-cmano-clone"));
        Assert.That(result.Query, Is.EqualTo("baltic roe"));
        Assert.That(result.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items[0].Text, Is.EqualTo("Keep WeaponsTight until contact classified"));
        Assert.That(result.Items[0].Score, Is.EqualTo(4.0));
        Assert.That(result.Items[0].Id, Is.EqualTo("mem-1"));
        Assert.That(result.Items[0].BankId, Is.EqualTo("dev-cmano-clone"));
        Assert.That(result.Items[0].Context, Is.EqualTo("aar"));
        Assert.That(result.Items[0].CreatedAt, Is.EqualTo("2026-11-22T00:00:00Z"));
    }

    [Test]
    public async Task RecallAsync_when_unreachable_returns_ProviderUnavailable()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("Connection refused"));
        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("http://localhost:8888/");
        var adapter = new HindsightRecallAdapter(http);

        var result = await adapter.RecallAsync(new HindsightRecallRequest("dev-cmano-clone", "roe"));

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.ProviderUnavailable));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Items, Is.Empty);
        Assert.That(result.Error, Does.Contain("Connection refused"));
    }

    [Test]
    public async Task RecallAsync_when_http_500_returns_ProviderUnavailable()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("http://localhost:8888/");
        var adapter = new HindsightRecallAdapter(http);

        var result = await adapter.RecallAsync(new HindsightRecallRequest("dev-cmano-clone", "roe"));

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.ProviderUnavailable));
        Assert.That(result.Error, Does.Contain("500"));
    }

    [Test]
    public async Task RecallAsync_when_timed_out_returns_TimedOut()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout."));
        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("http://localhost:8888/");
        var adapter = new HindsightRecallAdapter(http);

        var result = await adapter.RecallAsync(new HindsightRecallRequest("dev-cmano-clone", "roe"));

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.TimedOut));
        Assert.That(result.Items, Is.Empty);
    }

    [Test]
    public async Task RecallAsync_when_cancelled_by_caller_returns_Cancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new StubHttpMessageHandler((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("http://localhost:8888/");
        var adapter = new HindsightRecallAdapter(http);

        var result = await adapter.RecallAsync(new HindsightRecallRequest("dev-cmano-clone", "roe"), cts.Token);

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.Cancelled));
        Assert.That(result.Items, Is.Empty);
    }

    [TestCase("<<<not-json>>>")]
    [TestCase("{\"bankId\":\"b1\",\"query\":\"q\"}")]
    public async Task RecallAsync_when_malformed_json_or_missing_results_returns_MalformedOutput(string payload)
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload),
            }));
        using var http = new HttpClient(handler);
        http.BaseAddress = new Uri("http://localhost:8888/");
        var adapter = new HindsightRecallAdapter(http);

        var result = await adapter.RecallAsync(new HindsightRecallRequest("dev-cmano-clone", "roe"));

        Assert.That(result.Status, Is.EqualTo(GenerationStatus.MalformedOutput));
        Assert.That(result.Items, Is.Empty);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
