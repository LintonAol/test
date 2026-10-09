using System.ComponentModel.DataAnnotations;

namespace HackerNewsBestStories.Api.Configuration;

public sealed class HackerNewsOptions
{
    public const string SectionName = "HackerNews";

    /// <summary>Base address- Hacker News API - configured in appsettings.json.</summary>
    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Max simultaneous connections to Hacker News, it can be configured in appsettings.json.</summary>
    public int MaxConcurrentRequests { get; set; } = 10;
    /// <summary>Cache duration to best story IDs list from Hacker News, it can be configured in appsettings.json.</summary>
    /// <summary>Largest allowed <c>count</c>; Hacker News exposes at most 200 best stories.</summary>
    [Range(1, 200)]
    public int MaxStoryCount { get; set; } = 200;

    public TimeSpan BestStoryIdsCacheDuration { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>Cache duration to individual story details from Hacker News, it can be configured in appsettings.json.</summary>
    public TimeSpan StoryCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}
