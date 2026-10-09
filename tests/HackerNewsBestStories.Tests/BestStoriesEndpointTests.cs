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
    private readonly HttpClient _http;

    public BestStoriesEndpointTests(WebApplicationFactory<Program> factory)
    {
        var fake = new FakeHackerNewsClient(Item(1, 10), Item(2, 30), Item(3, 20));
        _http = factory
            .WithWebHostBuilder(b => b.ConfigureServices(s =>
            {
                s.RemoveAll<IHackerNewsClient>();
                s.AddSingleton<IHackerNewsClient>(fake);
            }))
            .CreateClient();
    }

    [Fact]
    public async Task Returns_best_stories_in_descending_score_order()
    {
        var stories = await _http.GetFromJsonAsync<List<BestStoryResponse>>("/api/stories/best?count=2");

        Assert.Equal(2, stories!.Count);
        Assert.True(stories[0].Score >= stories[1].Score);
    }

    [Fact]
    public async Task Serialises_with_the_documented_property_names()
    {
        var json = await _http.GetStringAsync("/api/stories/best?count=1");

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
    public async Task Rejects_invalid_count(string query)
    {
        var response = await _http.GetAsync("/api/stories/best" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
