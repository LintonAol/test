using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services;

public interface IHackerNewsClient
{
    /// <summary>Returns IDs of the best stories from Hacker News.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken);

    /// <summary>Returns item with specified ID from Hacker News.</summary>
    /// <param name="id">The ID of item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The item, or null when Hacker News has no such item.</returns>
    Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken);
}
