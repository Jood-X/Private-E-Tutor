using ETutor.Api.Common;
using ETutor.Api.Security;
using ETutor.Application.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Controllers;

/// <summary>Lessons and their resources in the teacher's own courses.</summary>
[Authorize(Policy = Policies.Teacher)]
[Route(ApiRoutes.V1 + "/teacher")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
public sealed class TeacherLessonsController(CourseContentService content, LessonResourceService resources) : ApiControllerBase
{
    /// <summary>Multipart overhead on top of the 20 MB document limit.</summary>
    private const long MaxUploadRequestBytes = LessonResourceService.MaxDocumentBytes + (1024 * 1024);

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType<ApiResponse<LessonDetailDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LessonDetailDto>>> Get(Guid lessonId, CancellationToken cancellationToken) =>
        FromResult(await content.GetLessonAsync(lessonId, cancellationToken));

    [HttpPut("lessons/{lessonId:guid}")]
    [ProducesResponseType<ApiResponse<LessonDetailDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LessonDetailDto>>> Update(
        Guid lessonId, LessonRequest request, CancellationToken cancellationToken) =>
        FromResult(await content.UpdateLessonAsync(lessonId, request, cancellationToken));

    [HttpPut("lessons/{lessonId:guid}/status")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> ChangeStatus(
        Guid lessonId, ChangeStatusRequest request, CancellationToken cancellationToken) =>
        FromResult(await content.ChangeLessonStatusAsync(lessonId, request.Status, cancellationToken));

    [HttpPut("lessons/{lessonId:guid}/position")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> Move(
        Guid lessonId, MoveRequest request, CancellationToken cancellationToken) =>
        FromResult(await content.MoveLessonAsync(lessonId, request.Position, cancellationToken));

    [HttpDelete("lessons/{lessonId:guid}")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> Delete(Guid lessonId, CancellationToken cancellationToken) =>
        FromResult(await content.DeleteLessonAsync(lessonId, cancellationToken));

    [HttpPost("lessons/{lessonId:guid}/resources/link")]
    [ProducesResponseType<ApiResponse<LessonDetailDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<LessonDetailDto>>> AddLink(
        Guid lessonId, LinkResourceRequest request, CancellationToken cancellationToken) =>
        FromResult(await resources.AddLinkAsync(lessonId, request, cancellationToken), StatusCodes.Status201Created);

    /// <summary>Uploads a PDF (multipart/form-data: file, title, isRequired). Max 20 MB.</summary>
    [HttpPost("lessons/{lessonId:guid}/resources/document")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
    [ProducesResponseType<ApiResponse<LessonDetailDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LessonDetailDto>>> AddDocument(
        Guid lessonId, IFormFile file, [FromForm] string title, [FromForm] bool isRequired, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        await using var stream = file.OpenReadStream();
        var upload = new DocumentUpload(title, isRequired, file.FileName, file.ContentType, file.Length, stream);
        return FromResult(await resources.AddDocumentAsync(lessonId, upload, cancellationToken), StatusCodes.Status201Created);
    }

    [HttpDelete("resources/{resourceId:guid}")]
    [ProducesResponseType<ApiResponse<LessonDetailDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LessonDetailDto>>> DeleteResource(Guid resourceId, CancellationToken cancellationToken) =>
        FromResult(await resources.DeleteAsync(resourceId, cancellationToken));
}
