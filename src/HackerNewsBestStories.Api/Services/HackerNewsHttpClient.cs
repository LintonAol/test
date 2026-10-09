using System.Net.Http.Json;
using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services;

/// <summary>Plain HTTP access to Hacker News, with no caching.</summary>
public sealed class HackerNewsHttpClient : IHackerNewsClient
{
    private readonly HttpClient _httpClient;

    public HackerNewsHttpClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        var ids = await _httpClient.GetFromJsonAsync<List<long>>("beststories.json", cancellationToken);
        return ids ?? [];
    }

    public Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken) =>
        _httpClient.GetFromJsonAsync<HackerNewsItem>($"item/{id}.json", cancellationToken);
}
