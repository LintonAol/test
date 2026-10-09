namespace HackerNewsBestStories.Api.Models;

public sealed record BestStoryResponse(
    string? Title,
    string? Uri,
    string? PostedBy,
    DateTimeOffset Time,
    int Score,
    int CommentCount);
