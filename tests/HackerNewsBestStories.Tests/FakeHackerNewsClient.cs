using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;

namespace HackerNewsBestStories.Tests;

/// <summary>In-memory Hacker News client for tests,counts calls and can be held open or made to fail.</summary>
internal sealed class FakeHackerNewsClient : IHackerNewsClient
{
    private readonly Dictionary<long, HackerNewsItem> _itemsById;
    private readonly List<long> _bestStoryIds;
    private int _idsCallCount;
    private int _itemCallCount;

    public FakeHackerNewsClient(params HackerNewsItem[] items)
    {
        _itemsById = items.ToDictionary(item => item.Id);
        _bestStoryIds = items.Select(item => item.Id).ToList();
    }

    /// <summary>While set every call waits for this to complete.</summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>When set every call throws this instead of returning data.</summary>
    public Exception? Failure { get; set; }

    public int IdsCallCount => _idsCallCount;

    public int ItemCallCount => _itemCallCount;

    public async Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _idsCallCount);
        await WaitForGateAsync();
        return _bestStoryIds;
    }

    public async Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _itemCallCount);
        await WaitForGateAsync();
        return _itemsById.GetValueOrDefault(id);
    }

    public static HackerNewsItem BuildStory(long id, int score) => new()
    {
        Id = id,
        Title = $"Story {id}",
        Url = $"https://example.com/{id}",
        By = "author",
        Time = 1570887781,
        Score = score,
        Descendants = 7,
    };

    private async Task WaitForGateAsync()
    {
        if (Gate is not null)
        {
            await Gate.Task;
        }

        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
