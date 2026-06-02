using System.Net;
using System.Text.Json;
using ProjectAegis.Delegation.Hindsight;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Hindsight;

[TestFixture]
public sealed class HindsightHttpMemoryClientTests
{
    [Test]
    public async Task RetainAsync_posts_to_memories_retain_endpoint()
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
                Content = new StringContent("{\"success\":true}"),
            };
        });

        var client = new HindsightHttpMemoryClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8888/") });

        await client.RetainAsync("agent-test-a1", "hello memory");

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(
            captured.RequestUri!.ToString(),
            Is.EqualTo("http://localhost:8888/v1/default/banks/agent-test-a1/memories/retain"));

        using var doc = JsonDocument.Parse(capturedBody!);
        Assert.That(doc.RootElement.GetProperty("async").GetBoolean(), Is.True);
        Assert.That(
            doc.RootElement.GetProperty("items")[0].GetProperty("content").GetString(),
            Is.EqualTo("hello memory"));
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
