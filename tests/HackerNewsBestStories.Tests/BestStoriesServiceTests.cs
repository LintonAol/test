using HackerNewsBestStories.Api.Services;
using static HackerNewsBestStories.Tests.FakeHackerNewsClient;

namespace HackerNewsBestStories.Tests;

public class BestStoriesServiceTests
{
    [Fact]
    public async Task Returns_requested_number_of_stories_sorted_by_score_descending()
    {
        var service = new BestStoriesService(new FakeHackerNewsClient(Item(1, 10), Item(2, 30), Item(3, 20)));

        var result = await service.GetBestStoriesAsync(3, CancellationToken.None);

        Assert.Equal([30, 20, 10], result.Select(s => s.Score));
    }

    [Fact]
    public async Task Fetches_only_the_first_n_ids()
    {
        var client = new FakeHackerNewsClient(Item(1, 10), Item(2, 30), Item(3, 20));
        var service = new BestStoriesService(client);

        var result = await service.GetBestStoriesAsync(2, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, client.ItemCalls);
    }

    [Fact]
    public async Task Returns_all_available_when_n_exceeds_available_stories()
    {
        var service = new BestStoriesService(new FakeHackerNewsClient(Item(1, 10)));

        var result = await service.GetBestStoriesAsync(50, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Skips_stories_that_no_longer_exist()
    {
        var client = new FakeHackerNewsClient(Item(1, 10), Item(2, 20));
        var service = new BestStoriesService(new MissingItemClient(client, missingId: 2));

        var result = await service.GetBestStoriesAsync(2, CancellationToken.None);

        Assert.Equal(10, Assert.Single(result).Score);
    }

    [Fact]
    public async Task Maps_item_fields_to_response()
    {
        var service = new BestStoriesService(new FakeHackerNewsClient(Item(1, 10)));

        var story = Assert.Single(await service.GetBestStoriesAsync(1, CancellationToken.None));

        Assert.Equal("Story 1", story.Title);
        Assert.Equal("https://example.com/1", story.Uri);
        Assert.Equal("author", story.PostedBy);
        Assert.Equal(DateTimeOffset.Parse("2019-10-12T13:43:01+00:00"), story.Time);
        Assert.Equal(10, story.Score);
        Assert.Equal(7, story.CommentCount);
    }

    private sealed class MissingItemClient(IHackerNewsClient inner, long missingId) : IHackerNewsClient
    {
        public Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken ct) => inner.GetBestStoryIdsAsync(ct);

        public Task<Api.Models.HackerNewsItem?> GetItemAsync(long id, CancellationToken ct) =>
            id == missingId ? Task.FromResult<Api.Models.HackerNewsItem?>(null) : inner.GetItemAsync(id, ct);
    }
}
