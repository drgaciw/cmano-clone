using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;
using NUnit.Framework;

namespace ProjectAegis.Ai.Host.Tests;

[TestFixture]
public sealed class FakeChatClientTests
{
    [TestCase(FakeChatScript.Success, GenerationStatus.Succeeded)]
    [TestCase(FakeChatScript.MalformedOutput, GenerationStatus.MalformedOutput)]
    [TestCase(FakeChatScript.Timeout, GenerationStatus.TimedOut)]
    [TestCase(FakeChatScript.Cancellation, GenerationStatus.Cancelled)]
    [TestCase(FakeChatScript.ProviderUnavailable, GenerationStatus.ProviderUnavailable)]
    public void ToGenerationStatus_maps_each_script(FakeChatScript script, GenerationStatus expected)
    {
        Assert.That(FakeChatClient.ToGenerationStatus(script), Is.EqualTo(expected));
    }

    [Test]
    public async Task Success_returns_assistant_json()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.Success);

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "ping")],
            new ChatOptions { ModelId = "unit" });

        Assert.That(response.ModelId, Is.EqualTo("unit"));
        Assert.That(response.Text, Does.Contain("ok"));
        Assert.That(FakeChatClient.ToGenerationStatus(FakeChatScript.Success), Is.EqualTo(GenerationStatus.Succeeded));
    }

    [Test]
    public async Task Malformed_returns_non_json_text()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.MalformedOutput);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]);
        Assert.That(response.Text, Is.EqualTo("<<<not-json>>>"));
    }

    [Test]
    public async Task Timeout_throws_TimeoutException()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.Timeout);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));
    }

    [Test]
    public async Task Cancellation_throws_OperationCanceledException()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.Cancellation);

        await Assert.CatchAsync<OperationCanceledException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));
    }

    [Test]
    public async Task ProviderUnavailable_throws_InvalidOperationException()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.ProviderUnavailable);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));
    }

    [Test]
    public async Task Empty_queue_throws()
    {
        using var client = new FakeChatClient();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));
    }
}
