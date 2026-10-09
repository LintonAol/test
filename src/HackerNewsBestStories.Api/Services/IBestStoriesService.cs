using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services;

public interface IBestStoriesService
{
    /// <summary>Returns up to <paramref name="count"/> best stories, highest score first.</summary>
    Task<IReadOnlyList<BestStoryResponse>> GetBestStoriesAsync(int count, CancellationToken cancellationToken);
}
