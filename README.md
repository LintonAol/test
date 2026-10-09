# Hacker News Best Stories API

ASP.NET Core (.NET 10) REST API that returns the top *n* Hacker News "best stories", ordered by score (highest first).

## Run

Requires the .NET 10 SDK.

```bash
dotnet run --project src/HackerNewsBestStories.Api --urls http://localhost:5246
curl "http://localhost:5246/api/stories/best?count=10"
```

## Test

```bash
dotnet test
```

## API

`GET /api/stories/best?count={n}`

| Status | Meaning |
|--------|---------|
| 200 | JSON array of up to `n` stories, descending by score |
| 400 | `count` missing or not an integer in `1..200` |
| 502 | Hacker News could not be reached |

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

## Design

```
BestStoriesController            HTTP, input validation, error mapping
        |
IBestStoriesService  ->  BestStoriesService      pick first n ids, fetch, sort, map
        |
IHackerNewsClient
  +- CachedHackerNewsClient      decorator: cache + request coalescing
        |
     HackerNewsHttpClient        plain HttpClient calls to Hacker News
```

- **Interfaces at each seam** (`IBestStoriesService`, `IHackerNewsClient`) so every layer is unit-testable with a hand-written fake (`FakeHackerNewsClient`); no mocking framework needed.
- **Decorator for caching** keeps `HackerNewsHttpClient` and `BestStoriesService` free of caching concerns.
- **Protecting Hacker News**
  - Best-story ids are cached for 1 minute, individual stories for 5 minutes (`HackerNews:BestStoryIdsCacheDuration`, `HackerNews:StoryCacheDuration`). Call volume to Hacker News is therefore independent of inbound request volume.
  - Concurrent requests for the same key share one in-flight call (stampede protection). A caller that cancels stops waiting but does not cancel the shared call.
  - A single shared `HttpClient` with `MaxConnectionsPerServer` (default 10) caps simultaneous outbound requests; extra calls queue.
- **Response**: the item `time` (Unix seconds) is exposed as an ISO 8601 `DateTimeOffset`; `descendants` is exposed as `commentCount`.
- Only the first `n` ids from `beststories.json` are fetched, then sorted by score locally to guarantee ordering.

### Configuration (`appsettings.json`, section `HackerNews`)

| Key | Default |
|-----|---------|
| `BaseUrl` | `https://hacker-news.firebaseio.com/v0/` |
| `MaxConcurrentRequests` | `10` |
| `BestStoryIdsCacheDuration` | `00:01:00` |
| `StoryCacheDuration` | `00:05:00` |

## Assumptions

- `count` is required and limited to 1-200, since `beststories.json` returns at most 200 ids.
- Scores/comment counts may be up to a few minutes stale; that trade-off is acceptable for a "best stories" list.
- Stories that are deleted/missing (`null` from Hacker News) are skipped, so fewer than `n` results may be returned.
- Stories without a URL (e.g. Ask HN) return `"uri": null`.
- Any Hacker News failure fails the whole request (502) rather than returning partial data.

## Enhancements with more time

- Distributed cache (e.g. Redis) when running multiple instances; serve stale data while refreshing in the background.
- Resilience policies (retry with jitter, circuit breaker, timeouts) via `Microsoft.Extensions.Http.Resilience`.
- Inbound rate limiting (`AddRateLimiter`) and response caching/ETags.
- Cache the assembled response per `count`.
- Health checks, structured logging/metrics, OpenAPI document, Dockerfile and CI pipeline.
- Contract tests against a recorded Hacker News payload.
