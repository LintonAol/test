using System.Text.Json.Serialization;

namespace HackerNewsBestStories.Api.Models;

/// <summary>A story as returned by the Hacker News item endpoint.</summary>
public sealed class HackerNewsItem
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("by")]
    public string? By { get; init; }

    /// <summary>Creation time as Unix seconds.</summary>
    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("score")]
    public int Score { get; init; }

    [JsonPropertyName("descendants")]
    public int Descendants { get; init; }
}
