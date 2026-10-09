using System.Collections.Concurrent;
using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNewsBestStories.Api.Services;

/// <summary>
/// Adds caching to the Hacker News client and avoids fetching the same data multiple times when several requests arrive together.
/// </summary>
public sealed class CachedHackerNewsClient : IHackerNewsClient
{
    private const string BestStoryIdsCacheKey = "best-story-ids";

    private readonly IHackerNewsClient _innerClient;
    private readonly IMemoryCache _memoryCache;
    private readonly HackerNewsOptions _options;
    private readonly ILogger<CachedHackerNewsClient> _logger;

    // Keep track of requests that are already fetching the same data - so that multiple requests for the same key can share the same task.
    private readonly ConcurrentDictionary<string, object> _inFlightRequests = new();

    public CachedHackerNewsClient(
        IHackerNewsClient innerClient,
        IMemoryCache memoryCache,
        IOptions<HackerNewsOptions> options,
        ILogger<CachedHackerNewsClient> logger)
    {
        _innerClient = innerClient;
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
    }

    #region Public methods

    public Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        return GetOrFetchAsync(
            BestStoryIdsCacheKey,
            _options.BestStoryIdsCacheDuration,
            () => _innerClient.GetBestStoryIdsAsync(CancellationToken.None),
            cancellationToken);
    }

    public Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken)
    {
        return GetOrFetchAsync(
            $"item:{id}",
            _options.StoryCacheDuration,
            () => _innerClient.GetItemAsync(id, CancellationToken.None),
            cancellationToken);
    }

    #endregion

    #region Private methods

    private async Task<T> GetOrFetchAsync<T>(
        string cacheKey,
        TimeSpan cacheDuration,
        Func<Task<T>> fetchData,
        CancellationToken cancellationToken)
    {
        // Return the cached value if we already have it.
        if (_memoryCache.TryGetValue(cacheKey, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for {CacheKey}.", cacheKey);
            return cachedValue!;
        }

        // If another request is fetching this key, reuse the same task.
        // Otherwise, start a new fetch and let other requests share it.
        var pendingRequest = (Lazy<Task<T>>)_inFlightRequests.GetOrAdd(
            cacheKey,
            _ => new Lazy<Task<T>>(
                () => FetchAndCacheAsync(cacheKey, cacheDuration, fetchData)));

        // A caller can stop waiting without cancelling the shared fetch
        // that other requests may still need.
        return await pendingRequest.Value.WaitAsync(cancellationToken);
    }

    private async Task<T> FetchAndCacheAsync<T>(
        string cacheKey,
        TimeSpan cacheDuration,
        Func<Task<T>> fetchData)
    {
        try
        {
            _logger.LogDebug("Cache miss for {CacheKey}; fetching from Hacker News.", cacheKey);
            var result = await fetchData();

            // Cache the result so the next request can return it directly.
            _memoryCache.Set(cacheKey, result, cacheDuration);

            return result;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Fetching {CacheKey} from Hacker News failed.", cacheKey);
            throw;
        }
        finally
        {
            // Remove the in-flight entry so a later request can fetch again after a failure or when the cached value expires.
            _inFlightRequests.TryRemove(cacheKey, out _);
        }
    }

    #endregion
}