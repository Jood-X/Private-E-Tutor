using ETutor.Api.Common;
using ETutor.Api.Security;
using ETutor.Application.Common;
using ETutor.Application.Enrollments;
using ETutor.Domain.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Controllers;

/// <summary>Enrollment requests and students of the teacher's own courses.</summary>
[Authorize(Policy = Policies.Teacher)]
[Route(ApiRoutes.V1 + "/teacher/enrollments")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
public sealed class TeacherEnrollmentsController(TeacherEnrollmentService enrollments) : ApiControllerBase
{
    /// <summary>Requests and students, newest-pending first. Filter by status and/or course; paginated.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<TeacherEnrollmentDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<TeacherEnrollmentDto>>>> List(
        [FromQuery] EnrollmentStatus? status,
        [FromQuery] Guid? courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        OkResponse(await enrollments.ListAsync(new EnrollmentQuery(status, courseId), new PageRequest(page, pageSize), cancellationToken));

    [HttpPost("{enrollmentId:guid}/approve")]
    [ProducesResponseType<ApiResponse<TeacherEnrollmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TeacherEnrollmentDto>>> Approve(
        Guid enrollmentId, DecisionRequest request, CancellationToken cancellationToken) =>
        FromResult(await enrollments.ApproveAsync(enrollmentId, request, cancellationToken));

    [HttpPost("{enrollmentId:guid}/reject")]
    [ProducesResponseType<ApiResponse<TeacherEnrollmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherEnrollmentDto>>> Reject(
        Guid enrollmentId, DecisionRequest request, CancellationToken cancellationToken) =>
        FromResult(await enrollments.RejectAsync(enrollmentId, request, cancellationToken));

    [HttpPost("{enrollmentId:guid}/revoke")]
    [ProducesResponseType<ApiResponse<TeacherEnrollmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherEnrollmentDto>>> Revoke(
        Guid enrollmentId, DecisionRequest request, CancellationToken cancellationToken) =>
        FromResult(await enrollments.RevokeAsync(enrollmentId, request, cancellationToken));
}
