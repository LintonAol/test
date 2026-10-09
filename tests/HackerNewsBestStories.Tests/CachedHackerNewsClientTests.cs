using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using static HackerNewsBestStories.Tests.FakeHackerNewsClient;

namespace HackerNewsBestStories.Tests;

public class CachedHackerNewsClientTests
{
    private static CachedHackerNewsClient CreateCachedClient(IHackerNewsClient inner) =>
        new(inner, new MemoryCache(new MemoryCacheOptions()), Options.Create(new HackerNewsOptions()));

    [Fact]
    public async Task Repeated_item_requests_hit_Hacker_News_once()
    {
        var inner = new FakeHackerNewsClient(Item(1, 10));
        var client = CreateCachedClient(inner);

        await client.GetItemAsync(1, CancellationToken.None);
        await client.GetItemAsync(1, CancellationToken.None);

        Assert.Equal(1, inner.ItemCalls);
    }

    [Fact]
    public async Task Repeated_id_requests_hit_Hacker_News_once()
    {
        var inner = new FakeHackerNewsClient(Item(1, 10));
        var client = CreateCachedClient(inner);

        await client.GetBestStoryIdsAsync(CancellationToken.None);
        await client.GetBestStoryIdsAsync(CancellationToken.None);

        Assert.Equal(1, inner.IdCalls);
    }

    [Fact]
    public async Task Concurrent_requests_for_the_same_item_share_one_call()
    {
        var gate = new TaskCompletionSource();
        var inner = new FakeHackerNewsClient(Item(1, 10)) { Gate = gate };
        var client = CreateCachedClient(inner);

        var calls = Enumerable.Range(0, 20).Select(_ => client.GetItemAsync(1, CancellationToken.None)).ToArray();
        gate.SetResult();
        await Task.WhenAll(calls);

        Assert.Equal(1, inner.ItemCalls);
    }

    [Fact]
    public async Task Cancelled_caller_stops_waiting_without_cancelling_the_shared_call()
    {
        var gate = new TaskCompletionSource();
        var inner = new FakeHackerNewsClient(Item(1, 10)) { Gate = gate };
        var client = CreateCachedClient(inner);
        using var cts = new CancellationTokenSource();

        var cancelled = client.GetItemAsync(1, cts.Token);
        var other = client.GetItemAsync(1, CancellationToken.None);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);

        gate.SetResult();
        Assert.NotNull(await other);
    }
}
