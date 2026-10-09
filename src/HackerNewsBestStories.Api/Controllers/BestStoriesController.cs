using HackerNewsBestStories.Api.Configuration;
using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HackerNewsBestStories.Api.Controllers;

/// <summary>Controller - handle requests for the best stories from Hacker News.</summary>

[ApiController]
[Route("api/stories/best")]
public sealed class BestStoriesController : ControllerBase
{
    private readonly IBestStoriesService _bestStories;
    private readonly ILogger<BestStoriesController> _logger;
    private readonly int _maxCount;

    public BestStoriesController(
        IBestStoriesService bestStories,
        IOptions<HackerNewsOptions> options,
        ILogger<BestStoriesController> logger)
    {
        _bestStories = bestStories;
        _logger = logger;
        _maxCount = options.Value.MaxStoryCount; // configuration from appsettings.json
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BestStoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Get([FromQuery] int? count, CancellationToken cancellationToken)
    {
        if (count is null or < 1 || count > _maxCount)
        {
            _logger.LogWarning("Rejected best stories request with invalid count {Count} (allowed 1-{MaxCount}).", count, _maxCount);
            ModelState.AddModelError(nameof(count), $"count must be an integer between 1 and {_maxCount}.");
            return ValidationProblem(ModelState);
        }

        try
        {
            return Ok(await _bestStories.GetBestStoriesAsync(count.Value, cancellationToken));
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Hacker News request failed while getting the best {Count} stories.", count);
            return Problem(
                title: "Hacker News is unavailable.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
