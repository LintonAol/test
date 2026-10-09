using System.Collections.Concurrent;
using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNewsBestStories.Api.Services;

/// <summary>
/// Decorates another client with an in-memory cache and de-duplication of concurrent
/// identical requests, so Hacker News sees at most one call per key per cache period.
/// </summary>
public sealed class CachedHackerNewsClient : IHackerNewsClient
{
    private const string BestStoryIdsKey = "best-story-ids";

    private readonly IHackerNewsClient _inner;
    private readonly IMemoryCache _cache;
    private readonly HackerNewsOptions _options;
    private readonly ConcurrentDictionary<string, object> _inFlight = new();

    public CachedHackerNewsClient(IHackerNewsClient inner, IMemoryCache cache, IOptions<HackerNewsOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _options = options.Value;
    }

    public Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken) =>
        GetOrFetchAsync(BestStoryIdsKey, _options.BestStoryIdsCacheDuration,
            () => _inner.GetBestStoryIdsAsync(CancellationToken.None), cancellationToken);

    public Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken) =>
        GetOrFetchAsync($"item:{id}", _options.StoryCacheDuration,
            () => _inner.GetItemAsync(id, CancellationToken.None), cancellationToken);

    private async Task<T> GetOrFetchAsync<T>(
        string key, TimeSpan duration, Func<Task<T>> fetch, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(key, out T? cached))
        {
            return cached!;
        }

        var lazy = (Lazy<Task<T>>)_inFlight.GetOrAdd(
            key, _ => new Lazy<Task<T>>(() => FetchAndCacheAsync(key, duration, fetch)));

        // The shared fetch ignores any single caller's token; each caller only stops waiting.
        return await lazy.Value.WaitAsync(cancellationToken);
    }

    private async Task<T> FetchAndCacheAsync<T>(string key, TimeSpan duration, Func<Task<T>> fetch)
    {
        try
        {
            var value = await fetch();
            _cache.Set(key, value, duration);
            return value;
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }
}
