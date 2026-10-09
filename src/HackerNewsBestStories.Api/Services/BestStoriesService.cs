using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services;

public sealed class BestStoriesService : IBestStoriesService
{
    private readonly IHackerNewsClient _hackerNews;

    public BestStoriesService(IHackerNewsClient hackerNews) => _hackerNews = hackerNews;

    public async Task<IReadOnlyList<BestStoryResponse>> GetBestStoriesAsync(
        int count, CancellationToken cancellationToken)
    {
        var ids = await _hackerNews.GetBestStoryIdsAsync(cancellationToken);

        var items = await Task.WhenAll(
            ids.Take(count).Select(id => _hackerNews.GetItemAsync(id, cancellationToken)));

        return items
            .OfType<HackerNewsItem>()
            .OrderByDescending(item => item.Score)
            .Select(ToResponse)
            .ToList();
    }

    private static BestStoryResponse ToResponse(HackerNewsItem item) => new(
        item.Title,
        item.Url,
        item.By,
        DateTimeOffset.FromUnixTimeSeconds(item.Time),
        item.Score,
        item.Descendants);
}
