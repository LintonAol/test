using System.Net;
using System.Net.Http.Json;
using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static HackerNewsBestStories.Tests.FakeHackerNewsClient;

namespace HackerNewsBestStories.Tests;

public class BestStoriesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BestStoriesUrl = "/api/stories/best";

    private readonly WebApplicationFactory<Program> _factory;

    public BestStoriesEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient BuildClient(IHackerNewsClient hackerNews, int? maxStoryCount = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            if (maxStoryCount is not null)
            {
                builder.UseSetting("HackerNews:MaxStoryCount", maxStoryCount.ToString());
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IHackerNewsClient>();
                services.AddSingleton(hackerNews);
            });
        }).CreateClient();

    private static FakeHackerNewsClient BuildThreeStoryFake() => new(BuildStory(1, 10), BuildStory(2, 30), BuildStory(3, 20));

    [Fact]
    public async Task BestStories_Get_WithValidCount_ReturnsFirstStoriesFromHackerNewsByScoreDescending()
    {
        var client = BuildClient(BuildThreeStoryFake());

        var stories = await client.GetFromJsonAsync<List<BestStoryResponse>>($"{BestStoriesUrl}?count=2");

        Assert.Equal([30, 10], stories!.Select(story => story.Score));
    }

    [Fact]
    public async Task BestStories_Get_ForAStory_UsesTheDocumentedJsonPropertyNames()
    {
        var client = BuildClient(BuildThreeStoryFake());

        var json = await client.GetStringAsync($"{BestStoriesUrl}?count=1");

        Assert.Contains("\"commentCount\":7", json);
        Assert.Contains("\"postedBy\":\"author\"", json);
        Assert.Contains("\"time\":\"2019-10-12T13:43:01+00:00\"", json);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?count=0")]
    [InlineData("?count=-1")]
    [InlineData("?count=201")]
    [InlineData("?count=abc")]
    public async Task BestStories_Get_WithMissingOrInvalidCount_ReturnsBadRequest(string query)
    {
        var client = BuildClient(BuildThreeStoryFake());

        var response = await client.GetAsync(BestStoriesUrl + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithCountAboveConfiguredMaximum_ReturnsBadRequest()
    {
        var client = BuildClient(BuildThreeStoryFake(), maxStoryCount: 2);

        var response = await client.GetAsync($"{BestStoriesUrl}?count=3");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task BestStories_Get_WithCountEqualToConfiguredMaximum_ReturnsOk()
    {
        var client = BuildClient(BuildThreeStoryFake(), maxStoryCount: 2);

        var response = await client.GetAsync($"{BestStoriesUrl}?count=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task BestStories_Get_WhenHackerNewsIsUnavailable_ReturnsBadGateway()
    {
        var failingFake = new FakeHackerNewsClient(BuildStory(1, 10)) { Failure = new HttpRequestException("down") };
        var client = BuildClient(failingFake);

        var response = await client.GetAsync($"{BestStoriesUrl}?count=1");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
