using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;

namespace HackerNewsBestStories.Tests;

/// <summary>In-memory stand-in for Hacker News that records how often it is called.</summary>
internal sealed class FakeHackerNewsClient : IHackerNewsClient
{
    private readonly Dictionary<long, HackerNewsItem> _items;
    private readonly List<long> _ids;
    private int _idCalls;
    private int _itemCalls;

    public FakeHackerNewsClient(params HackerNewsItem[] items)
    {
        _items = items.ToDictionary(item => item.Id);
        _ids = items.Select(item => item.Id).ToList();
    }

    public TaskCompletionSource? Gate { get; set; }
    public int IdCalls => _idCalls;
    public int ItemCalls => _itemCalls;

    public async Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _idCalls);
        if (Gate is not null) await Gate.Task;
        return _ids;
    }

    public async Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _itemCalls);
        if (Gate is not null) await Gate.Task;
        return _items.GetValueOrDefault(id);
    }

    public static HackerNewsItem Item(long id, int score) => new()
    {
        Id = id,
        Title = $"Story {id}",
        Url = $"https://example.com/{id}",
        By = "author",
        Time = 1570887781,
        Score = score,
        Descendants = 7,
    };
}
