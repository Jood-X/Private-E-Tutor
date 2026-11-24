using ETutor.Api.Common;
using ETutor.Application.Learning;
using ETutor.Application.Videos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Controllers;

/// <summary>
/// Lesson documents and video playback for the course's teacher or an approved student. The file endpoint answers
/// with the file itself (not the JSON envelope) on success; failures still use the envelope.
/// </summary>
[Authorize]
[Route(ApiRoutes.V1 + "/resources")]
public sealed class ResourcesController(LearningService learning) : ApiControllerBase
{
    [HttpGet("{resourceId:guid}/file")]
    [Produces("application/pdf", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid resourceId, CancellationToken cancellationToken)
    {
        var result = await learning.OpenDocumentAsync(resourceId, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromResult(result).Result!;
        }

        // Private content: never cache in shared caches.
        Response.Headers.CacheControl = "private, no-store";
        var file = result.Value;
        return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }

    /// <summary>What the in-app player needs to play a video resource (provider and its video ID).</summary>
    [HttpGet("{resourceId:guid}/playback")]
    [ProducesResponseType<ApiResponse<VideoPlaybackDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<VideoPlaybackDto>>> Playback(Guid resourceId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        return FromResult(await learning.GetPlaybackAsync(resourceId, cancellationToken));
    }
}
