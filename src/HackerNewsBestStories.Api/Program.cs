using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddOptions<HackerNewsOptions>()
    .Bind(builder.Configuration.GetSection(HackerNewsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// A single shared HttpClient; the connection cap throttles outbound load on Hacker News.
builder.Services.AddSingleton<IHackerNewsClient>(provider =>
{
    var options = provider.GetRequiredService<IOptions<HackerNewsOptions>>();
    var httpClient = new HttpClient(new SocketsHttpHandler
    {
        MaxConnectionsPerServer = options.Value.MaxConcurrentRequests,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    })
    {
        BaseAddress = new Uri(options.Value.BaseUrl),
    };

    return new CachedHackerNewsClient(
        new HackerNewsHttpClient(httpClient),
        provider.GetRequiredService<IMemoryCache>(),
        options,
        provider.GetRequiredService<ILogger<CachedHackerNewsClient>>());
});
builder.Services.AddScoped<IBestStoriesService, BestStoriesService>();

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program;
