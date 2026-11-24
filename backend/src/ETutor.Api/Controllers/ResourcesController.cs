using ETutor.Api.Common;
using ETutor.Application.Learning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Controllers;

/// <summary>
/// Streams lesson documents to the course's teacher or to an approved student. Answers with the file itself
/// (not the JSON envelope) on success; failures still use the envelope.
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
}
