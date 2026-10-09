using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using static HackerNewsBestStories.Tests.FakeHackerNewsClient;

namespace HackerNewsBestStories.Tests;

public class BestStoriesServiceTests
{
    private static BestStoriesService BuildService(IHackerNewsClient hackerNews) =>
        new(hackerNews, NullLogger<BestStoriesService>.Instance);

    [Fact]
    public async Task GetBestStoriesAsync_WhenIdsAreNotInScoreOrder_ReturnsStoriesByScoreDescending()
    {
        var service = BuildService(
            new FakeHackerNewsClient(BuildStory(1, 10), BuildStory(2, 30), BuildStory(3, 20)));

        var stories = await service.GetBestStoriesAsync(3, CancellationToken.None);

        Assert.Equal([30, 20, 10], stories.Select(story => story.Score));
    }
    [Fact]
    public async Task BestStoriesService_GetBestStoriesAsync_WhenCountIsBelowAvailable_FetchesOnlyThatManyStories()
    {
        var hackerNews = new FakeHackerNewsClient(BuildStory(1, 10), BuildStory(2, 30), BuildStory(3, 20));
        var service = BuildService(hackerNews);

        var stories = await service.GetBestStoriesAsync(2, CancellationToken.None);

        Assert.Equal(2, stories.Count);
        Assert.Equal(2, hackerNews.ItemCallCount);
    }
    [Fact]
    public async Task BestStoriesService_GetBestStoriesAsync_WhenCountExceedsAvailable_ReturnsEverythingAvailable()
    {
        var service = BuildService(new FakeHackerNewsClient(BuildStory(1, 10)));

        var stories = await service.GetBestStoriesAsync(50, CancellationToken.None);

        Assert.Single(stories);
    }
    [Fact]
    public async Task BestStoriesService_GetBestStoriesAsync_WhenAStoryHasBeenRemoved_LeavesItOut()
    {
        var hackerNews = new FakeHackerNewsClient(BuildStory(1, 10), BuildStory(2, 20));
        var service = BuildService(new RemovedStoryClient(hackerNews, removedId: 2));

        var stories = await service.GetBestStoriesAsync(2, CancellationToken.None);

        Assert.Equal(10, Assert.Single(stories).Score);
    }
    [Fact]
    public async Task BestStoriesService_GetBestStoriesAsync_WhenHackerNewsFails_PropagatesTheException()
    {
        var hackerNews = new FakeHackerNewsClient(BuildStory(1, 10)) { Failure = new HttpRequestException("down") };
        var service = BuildService(hackerNews);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetBestStoriesAsync(1, CancellationToken.None));
    }
    [Fact]
    public async Task BestStoriesService_GetBestStoriesAsync_ForAStory_MapsHackerNewsFieldsToResponse()
    {
        var service = BuildService(new FakeHackerNewsClient(BuildStory(1, 10)));

        var story = Assert.Single(await service.GetBestStoriesAsync(1, CancellationToken.None));

        Assert.Equal("Story 1", story.Title);
        Assert.Equal("https://example.com/1", story.Uri);
        Assert.Equal("author", story.PostedBy);
        Assert.Equal(DateTimeOffset.Parse("2019-10-12T13:43:01+00:00"), story.Time);
        Assert.Equal(10, story.Score);
        Assert.Equal(7, story.CommentCount);
    }

    private sealed class RemovedStoryClient(IHackerNewsClient inner, long removedId) : IHackerNewsClient
    {
        public Task<IReadOnlyList<long>> GetBestStoryIdsAsync(CancellationToken cancellationToken) =>
            inner.GetBestStoryIdsAsync(cancellationToken);

        public Task<HackerNewsItem?> GetItemAsync(long id, CancellationToken cancellationToken) =>
            id == removedId ? Task.FromResult<HackerNewsItem?>(null) : inner.GetItemAsync(id, cancellationToken);
    }
}
