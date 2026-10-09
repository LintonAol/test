namespace HackerNewsBestStories.Api.Configuration;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    /// <summary>Base address of the Hacker News API; must end with '/'.</summary>
    public string BaseUrl { get; set; } = "https://hacker-news.firebaseio.com/v0/";

    /// <summary>Upper bound on simultaneous connections to Hacker News.</summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    public TimeSpan BestStoryIdsCacheDuration { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan StoryCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}
