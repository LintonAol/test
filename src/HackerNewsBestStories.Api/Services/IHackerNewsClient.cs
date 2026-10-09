using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services;

public interface IHackerNewsClient
{
    Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken);

    /// <returns>The item, or null when Hacker News has no such item.</returns>
    Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken);
}
