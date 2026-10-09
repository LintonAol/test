using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static HackerNewsBestStories.Tests.FakeHackerNewsClient;

namespace HackerNewsBestStories.Tests;

public class CachedHackerNewsClientTests
{
    private static CachedHackerNewsClient BuildCachedClient(IHackerNewsClient innerClient) =>
        new(
            innerClient,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new HackerNewsOptions()),
            NullLogger<CachedHackerNewsClient>.Instance);

    [Fact]
    public async Task CachedHackerNewsClient_GetItemAsync_CalledTwiceForSameId_CallsHackerNewsOnce()
    {
        var innerClient = new FakeHackerNewsClient(BuildStory(1, 10));
        var cachedClient = BuildCachedClient(innerClient);

        await cachedClient.GetItemAsync(1, CancellationToken.None);
        await cachedClient.GetItemAsync(1, CancellationToken.None);

        Assert.Equal(1, innerClient.ItemCallCount);
    }

    [Fact]
    public async Task CachedHackerNewsClient_GetItemAsync_ForDifferentIds_CallsHackerNewsForEach()
    {
        var innerClient = new FakeHackerNewsClient(BuildStory(1, 10), BuildStory(2, 20));
        var cachedClient = BuildCachedClient(innerClient);

        await cachedClient.GetItemAsync(1, CancellationToken.None);
        await cachedClient.GetItemAsync(2, CancellationToken.None);

        Assert.Equal(2, innerClient.ItemCallCount);
    }

    [Fact]
    public async Task CachedHackerNewsClient_GetBestStoryIdsAsync_CalledTwice_CallsHackerNewsOnce()
    {
        var innerClient = new FakeHackerNewsClient(BuildStory(1, 10));
        var cachedClient = BuildCachedClient(innerClient);

        await cachedClient.GetBestStoryIdsAsync(CancellationToken.None);
        await cachedClient.GetBestStoryIdsAsync(CancellationToken.None);

        Assert.Equal(1, innerClient.IdsCallCount);
    }

    [Fact]
    public async Task CachedHackerNewsClient_GetItemAsync_WhenManyCallersArriveTogether_SharesOneHackerNewsCall()
    {
        var gate = new TaskCompletionSource();
        var innerClient = new FakeHackerNewsClient(BuildStory(1, 10)) { Gate = gate };
        var cachedClient = BuildCachedClient(innerClient);

        var pendingCalls = Enumerable.Range(0, 20)
            .Select(_ => cachedClient.GetItemAsync(1, CancellationToken.None))
            .ToArray();
        gate.SetResult();
        await Task.WhenAll(pendingCalls);

        Assert.Equal(1, innerClient.ItemCallCount);
    }

    [Fact]
    public async Task CachedHackerNewsClient_GetItemAsync_WhenOneCallerCancels_OtherCallersStillGetTheStory()
    {
        var gate = new TaskCompletionSource();
        var innerClient = new FakeHackerNewsClient(BuildStory(1, 10)) { Gate = gate };
        var cachedClient = BuildCachedClient(innerClient);
        using var cancellation = new CancellationTokenSource();

        var cancelledCall = cachedClient.GetItemAsync(1, cancellation.Token);
        var otherCall = cachedClient.GetItemAsync(1, CancellationToken.None);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledCall);

        gate.SetResult();
        Assert.NotNull(await otherCall);
    }

    [Fact]
    public async Task CachedHackerNewsClient_GetItemAsync_AfterHackerNewsFailure_RetriesInsteadOfCachingTheFailure()
    {
        var innerClient = new FakeHackerNewsClient(BuildStory(1, 10)) { Failure = new HttpRequestException("down") };
        var cachedClient = BuildCachedClient(innerClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => cachedClient.GetItemAsync(1, CancellationToken.None));
        innerClient.Failure = null;
        var story = await cachedClient.GetItemAsync(1, CancellationToken.None);

        Assert.NotNull(story);
        Assert.Equal(2, innerClient.ItemCallCount);
    }
}
