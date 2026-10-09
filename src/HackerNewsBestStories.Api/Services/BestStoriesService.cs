using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services;

public sealed class BestStoriesService : IBestStoriesService
{
    
    private readonly IHackerNewsClient _hackerNews;
    private readonly ILogger<BestStoriesService> _logger;
    
    #region Public methods
    public BestStoriesService(IHackerNewsClient hackerNews, ILogger<BestStoriesService> logger)
    {
        _hackerNews = hackerNews;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BestStoryResponse>> GetBestStoriesAsync(int count, CancellationToken cancellationToken)
    {
        var ids = await _hackerNews.GetBestStoryIdsAsync(cancellationToken);
        var items = await Task.WhenAll(ids.Take(count).Select(id => _hackerNews.GetItemAsync(id, cancellationToken)));

        var stories = items
            .OfType<HackerNewsItem>()
            .OrderByDescending(item => item.Score)
            .Select(ToResponseInformation)
            .ToList();

        _logger.LogInformation("Returning {ReturnedCount} of {RequestedCount} requested best stories.", stories.Count, count);
        return stories;
    }
   #endregion

    #region Private methods
    private static BestStoryResponse ToResponseInformation(HackerNewsItem item) 
    => new(
        item.Title,
        item.Url,
        item.By,
        DateTimeOffset.FromUnixTimeSeconds(item.Time),
        item.Score,
        item.Descendants);
    #endregion
}
