using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host;
using Xunit;

namespace ProjectAegis.Ai.Host.Tests;

public sealed class FakeChatClientTests
{
    [Theory]
    [InlineData(FakeChatScript.Success, GenerationStatus.Succeeded)]
    [InlineData(FakeChatScript.MalformedOutput, GenerationStatus.MalformedOutput)]
    [InlineData(FakeChatScript.Timeout, GenerationStatus.TimedOut)]
    [InlineData(FakeChatScript.Cancellation, GenerationStatus.Cancelled)]
    [InlineData(FakeChatScript.ProviderUnavailable, GenerationStatus.ProviderUnavailable)]
    public void ToGenerationStatus_maps_each_script(FakeChatScript script, GenerationStatus expected)
    {
        Assert.Equal(expected, FakeChatClient.ToGenerationStatus(script));
    }

    [Fact]
    public async Task Success_returns_assistant_json()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.Success);

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "ping")],
            new ChatOptions { ModelId = "unit" });

        Assert.Equal("unit", response.ModelId);
        Assert.Contains("ok", response.Text, StringComparison.Ordinal);
        Assert.Equal(GenerationStatus.Succeeded, FakeChatClient.ToGenerationStatus(FakeChatScript.Success));
    }

    [Fact]
    public async Task Malformed_returns_non_json_text()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.MalformedOutput);

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]);
        Assert.Equal("<<<not-json>>>", response.Text);
    }

    [Fact]
    public async Task Timeout_throws_TimeoutException()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.Timeout);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));
    }

    [Fact]
    public async Task Cancellation_throws_OperationCanceledException()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.Cancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));
    }

    [Fact]
    public async Task ProviderUnavailable_throws_InvalidOperationException()
    {
        using var client = new FakeChatClient();
        client.Enqueue(FakeChatScript.ProviderUnavailable);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));
    }

    [Fact]
    public async Task Empty_queue_throws()
    {
        using var client = new FakeChatClient();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new ChatMessage(ChatRole.User, "ping")]));
    }
}
