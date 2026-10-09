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

## Testing - Main scenario validations
1. Get the top 10 stories - curl "http://localhost:5246/api/stories/best?count=10"

Reponse:
```json
[{"title":"Margaret Hamilton has died","uri":"https://news.mit.edu/2026/margaret-hamilton-computing-pioneer-dies-1007","postedBy":"muglug","time":"2026-10-07T21:16:18+00:00","score":2093,"commentCount":250},{"title":"Claude Haiku 5.5","uri":"https://www.anthropic.com/claude-haiku-5-5","postedBy":"sfkgtbor","time":"2026-10-07T18:01:32+00:00","score":1039,"commentCount":485},{"title":"Trump administration is suspending Microsoft from a green card program","uri":"https://apnews.com/article/h1b-visa-program-vance-microsoft-e7b3a407f822702b269ee277d21343ea","postedBy":"alephnerd","time":"2026-10-08T15:15:00+00:00","score":881,"commentCount":1503},{"title":"Why isn't the industry freaking out about DeepSeek 4.1 Flash?","uri":"https://www.dgt.is/blog/2026-10-07-deepseek-freek-out/","postedBy":"jonotime","time":"2026-10-08T00:14:48+00:00","score":825,"commentCount":735},{"title":"Whistle: Speech to Text in 16.9 MB","uri":"https://cactuscompute.com/blog/whistle","postedBy":"gmays","time":"2026-10-08T16:59:39+00:00","score":798,"commentCount":159},{"title":"Man discovers his parents' coffee machine used 1TB of data in 10 days","uri":"https://www.dexerto.com/entertainment/man-discovers-his-parents-coffee-machine-used-1tb-of-data-in-10-days-3416399/","postedBy":"ck2","time":"2026-10-07T16:56:30+00:00","score":750,"commentCount":460},{"title":"GPT‑6 and Intelligent UI for everyone","uri":"https://openai.com/index/gpt-6-for-everyone/","postedBy":"joshuawright11","time":"2026-10-07T18:00:58+00:00","score":749,"commentCount":455},{"title":"Tell HN: I've been paying for a rural Tanzanian's education for 10 years","uri":null,"postedBy":"lukehandcool","time":"2026-10-08T14:39:47+00:00","score":709,"commentCount":219},{"title":"Show HN: Bigwords.page – Turn any screen into a sign. The URL is the app","uri":"https://bigwords.page/","postedBy":"SpeakingOfBrad","time":"2026-10-07T15:44:21+00:00","score":703,"commentCount":162},{"title":"“Math 2.0” will need to value mathematical progress more holistically","uri":"https://mathstodon.xyz/@tao/117395269325940185","postedBy":"ent101","time":"2026-10-08T05:14:14+00:00","score":598,"commentCount":636}
```

2. Get the top 201th stories - out of range - curl "http://localhost:5246/api/stories/best?count=201"

Reponse:
```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"count":["count must be an integer between 1 and 200."]},"traceId":"00-fd0fd54abba412a2a7a05d83180fd23a-1496e477764730a9-00"}
```

3. Missing or non-numeric count in url - curl "http://localhost:5246/api/stories/best?count=abc"

Reponse:
```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,"errors":{"count":["The value 'abc' is not valid."]},"traceId":"00-df4668a3669749d68d9f91312bf46a38-e25bad24667d3430-00"}
```
