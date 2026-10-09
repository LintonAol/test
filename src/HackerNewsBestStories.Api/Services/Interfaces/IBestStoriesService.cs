using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services;

public interface IBestStoriesService
{
    /// <summary>Returns up to n(<paramref name="count"/>) best stories, highest score first.</summary>
    /// <param name="count">The number of best stories to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IReadOnlyList<BestStoryResponse>> GetBestStoriesAsync(int count, CancellationToken cancellationToken);
}
