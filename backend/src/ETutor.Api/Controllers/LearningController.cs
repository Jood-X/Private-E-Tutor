using ETutor.Api.Common;
using ETutor.Api.Security;
using ETutor.Application.Common;
using ETutor.Application.Courses;
using ETutor.Application.Discovery;
using ETutor.Application.Enrollments;
using ETutor.Application.Learning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Controllers;

/// <summary>Student side: find teachers, request enrollment, and view enrolled courses.</summary>
[Authorize(Policy = Policies.Student)]
[Route(ApiRoutes.V1)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
public sealed class LearningController(
    DiscoveryService discovery,
    EnrollmentService enrollments,
    LearningService learning) : ApiControllerBase
{
    [HttpGet("teachers")]
    [ProducesResponseType<ApiResponse<PagedResult<TeacherCardDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<TeacherCardDto>>>> SearchTeachers(
        [FromQuery] int? curriculumId,
        [FromQuery] int? gradeLevelId,
        [FromQuery] int? subjectId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        OkResponse(await discovery.SearchAsync(
            new TeacherSearch(curriculumId, gradeLevelId, subjectId, search), new PageRequest(page, pageSize), cancellationToken));

    [HttpGet("teachers/{teacherId:guid}")]
    [ProducesResponseType<ApiResponse<TeacherDetailDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TeacherDetailDto>>> GetTeacher(Guid teacherId, CancellationToken cancellationToken) =>
        FromResult(await discovery.GetTeacherAsync(teacherId, cancellationToken));

    [HttpPost("courses/{courseId:guid}/enrollments")]
    [ProducesResponseType<ApiResponse<MyEnrollmentDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<MyEnrollmentDto>>> RequestEnrollment(
        Guid courseId, EnrollmentRequest request, CancellationToken cancellationToken) =>
        FromResult(await enrollments.RequestAsync(courseId, request, cancellationToken), StatusCodes.Status201Created);

    [HttpGet("learning/enrollments")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<MyEnrollmentDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MyEnrollmentDto>>>> MyEnrollments(CancellationToken cancellationToken) =>
        OkResponse(await enrollments.ListMineAsync(cancellationToken));

    [HttpPost("learning/enrollments/{enrollmentId:guid}/withdraw")]
    [ProducesResponseType<ApiResponse<MyEnrollmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MyEnrollmentDto>>> Withdraw(Guid enrollmentId, CancellationToken cancellationToken) =>
        FromResult(await enrollments.WithdrawAsync(enrollmentId, cancellationToken));

    [HttpGet("learning/courses/{courseId:guid}")]
    [ProducesResponseType<ApiResponse<StudentCourseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<StudentCourseDto>>> GetCourse(Guid courseId, CancellationToken cancellationToken) =>
        FromResult(await learning.GetCourseAsync(courseId, cancellationToken));

    [HttpGet("learning/lessons/{lessonId:guid}")]
    [ProducesResponseType<ApiResponse<LessonDetailDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LessonDetailDto>>> GetLesson(Guid lessonId, CancellationToken cancellationToken) =>
        FromResult(await learning.GetLessonAsync(lessonId, cancellationToken));
}
