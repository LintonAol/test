using System.ComponentModel.DataAnnotations;
using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HackerNewsBestStories.Api.Controllers;

[ApiController]
[Route("api/stories/best")]
public sealed class BestStoriesController : ControllerBase
{
    /// <summary>Hacker News exposes at most 200 best stories.</summary>
    public const int MaxCount = 200;

    private readonly IBestStoriesService _bestStories;

    public BestStoriesController(IBestStoriesService bestStories) => _bestStories = bestStories;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BestStoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Get(
        [FromQuery, Required, Range(1, MaxCount)] int? count, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _bestStories.GetBestStoriesAsync(count!.Value, cancellationToken));
        }
        catch (HttpRequestException)
        {
            return Problem(
                title: "Hacker News is unavailable.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
