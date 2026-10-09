using System.Net;
using HackerNewsBestStories.Api.Services;

namespace HackerNewsBestStories.Tests;

public class HackerNewsHttpClientTests
{
    private static HackerNewsHttpClient BuildClient(HttpStatusCode status, string body, List<string>? requestedPaths = null)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedPaths?.Add(request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        });

        return new HackerNewsHttpClient(new HttpClient(handler) { BaseAddress = new Uri("https://hn.test/v0/") });
    }
    [Fact]
    public async Task HackerNewsHttpClient_GetBestStoryIdsAsync_ReadsIdsFromBestStoriesEndpoint()
    {
        var requestedPaths = new List<string>();
        var client = BuildClient(HttpStatusCode.OK, "[11,22,33]", requestedPaths);

        var ids = await client.GetBestStoryIdsAsync(CancellationToken.None);

        Assert.Equal([11L, 22L, 33L], ids);
        Assert.Equal("/v0/beststories.json", Assert.Single(requestedPaths));
    }
    [Fact]
    public async Task HackerNewsHttpClient_GetBestStoryIdsAsync_WhenBodyIsNull_ReturnsEmptyList()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");

        Assert.Empty(await client.GetBestStoryIdsAsync(CancellationToken.None));
    }
    [Fact]
    public async Task HackerNewsHttpClient_GetItemAsync_ReadsStoryFromItemEndpoint()
    {
        var requestedPaths = new List<string>();
        var client = BuildClient(
            HttpStatusCode.OK,
            """{"id":21233041,"title":"T","url":"https://u","by":"me","time":1570887781,"score":1716,"descendants":572}""",
            requestedPaths);

        var item = await client.GetItemAsync(21233041, CancellationToken.None);

        Assert.Equal("/v0/item/21233041.json", Assert.Single(requestedPaths));
        Assert.NotNull(item);
        Assert.Equal("me", item.By);
        Assert.Equal(1716, item.Score);
        Assert.Equal(572, item.Descendants);
    }

    [Fact]
    public async Task HackerNewsHttpClient_GetItemAsync_WhenHackerNewsHasNoSuchItem_ReturnsNull()
    {
        var client = BuildClient(HttpStatusCode.OK, "null");
        Assert.Null(await client.GetItemAsync(1, CancellationToken.None));
    }
    [Fact]
    public async Task HackerNewsHttpClient_GetItemAsync_WhenHackerNewsReturnsServerError_ThrowsHttpRequestException()
    {
        var client = BuildClient(HttpStatusCode.InternalServerError, "");
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetItemAsync(1, CancellationToken.None));
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
