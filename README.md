# Hacker News Best Stories API

A REST API built with ASP.NET Core and .NET 10 to return the top `n` Hacker News stories, ordered by score.

## Run

Requires the .NET 10 SDK.

```bash
./run.sh
```

The API runs on port `5246`.

## Use the API

Get the top 10 stories:

```bash
curl "http://localhost:5246/api/stories/best?count=10"
```

Get the top 5 stories:

```bash
curl "http://localhost:5246/api/stories/best?count=5"
```

The `count` parameter is required and must be between `1` and `200` by default. Added validation for it.

In GitHub Codespaces(if this is env), open port `5246` and use the forwarded URL instead of `localhost`.

## Tests

Run the tests with:

```bash
./run.sh test
```

Or run direct cmdline:

```bash
dotnet test
```

## API responses

| Status | Description |
|---|---|
| `200 OK` | Returns stories ordered by score |
| `400 Bad Request` | Missing or invalid `count` |
| `502 Bad Gateway` | Hacker News API request failed |
If any error, it will be logged. There is no pattern given for the errorcode.

Example response:

```json
[
  {
    "title": "Example story",
    "uri": "https://example.com/story",
    "postedBy": "username",
    "time": "2026-01-01T12:00:00+00:00",
    "score": 100,
    "commentCount": 25
  }
]
```

## Design

- **Controller:** Handles requests, validation and HTTP responses.
- **Service:** Fetches story details, sorts by score and maps the response.
- **HTTP client:** Calls the Hacker News API.
- **Caching decorator:** Caches results and avoids duplicate concurrent requests.

Interfaces keep the service and client easy to test. Tests use a simple fake client without a mocking framework.

## Configuration

The API settings are defined in `appsettings.json` under the `HackerNews` section.

| Setting | Value | Purpose |
|---|---|---|
| `BaseUrl` | `https://hacker-news.firebaseio.com/v0/` | Hacker News API base URL |
| `MaxConcurrentRequests` | `10` | Configured limit for outbound requests |
| `MaxStoryCount` | `200` | Maximum number of stories requested |
| `BestStoryIdsCacheDuration` | `00:01:00` | Cache duration for best-story IDs (1 minute) |
| `StoryCacheDuration` | `00:05:00` | Cache duration for individual stories (5 minutes) |


Deleted or missing stories are skipped, so the API may return fewer than the requested number. Stories without a URL return `null` for `uri`.

## Possible improvements

- Add Redis for caching across multiple instances.
- Add retries, timeouts and a circuit breaker.


