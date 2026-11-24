using ETutor.Api.Common;
using ETutor.Api.Security;
using ETutor.Application.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Controllers;

/// <summary>A teacher's own courses and units. Items of other teachers answer 404.</summary>
[Authorize(Policy = Policies.Teacher)]
[Route(ApiRoutes.V1 + "/teacher")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
public sealed class TeacherCoursesController(TeacherCourseService courses, CourseContentService content) : ApiControllerBase
{
    [HttpGet("courses")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<TeacherCourseSummaryDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TeacherCourseSummaryDto>>>> List(CancellationToken cancellationToken) =>
        OkResponse(await courses.ListMineAsync(cancellationToken));

    [HttpPost("courses")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> Create(CreateCourseRequest request, CancellationToken cancellationToken) =>
        FromResult(await courses.CreateAsync(request, cancellationToken), StatusCodes.Status201Created);

    [HttpGet("courses/{courseId:guid}")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> Get(Guid courseId, CancellationToken cancellationToken) =>
        FromResult(await courses.GetAsync(courseId, cancellationToken));

    [HttpPut("courses/{courseId:guid}")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> Update(
        Guid courseId, UpdateCourseRequest request, CancellationToken cancellationToken) =>
        FromResult(await courses.UpdateAsync(courseId, request, cancellationToken));

    [HttpPut("courses/{courseId:guid}/status")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> ChangeStatus(
        Guid courseId, ChangeStatusRequest request, CancellationToken cancellationToken) =>
        FromResult(await courses.ChangeStatusAsync(courseId, request.Status, cancellationToken));

    [HttpPost("courses/{courseId:guid}/units")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> CreateUnit(
        Guid courseId, UnitRequest request, CancellationToken cancellationToken) =>
        FromResult(await content.CreateUnitAsync(courseId, request, cancellationToken), StatusCodes.Status201Created);

    [HttpPut("units/{unitId:guid}")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> UpdateUnit(
        Guid unitId, UnitRequest request, CancellationToken cancellationToken) =>
        FromResult(await content.UpdateUnitAsync(unitId, request, cancellationToken));

    [HttpPut("units/{unitId:guid}/status")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> ChangeUnitStatus(
        Guid unitId, ChangeStatusRequest request, CancellationToken cancellationToken) =>
        FromResult(await content.ChangeUnitStatusAsync(unitId, request.Status, cancellationToken));

    [HttpPut("units/{unitId:guid}/position")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> MoveUnit(
        Guid unitId, MoveRequest request, CancellationToken cancellationToken) =>
        FromResult(await content.MoveUnitAsync(unitId, request.Position, cancellationToken));

    [HttpDelete("units/{unitId:guid}")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> DeleteUnit(Guid unitId, CancellationToken cancellationToken) =>
        FromResult(await content.DeleteUnitAsync(unitId, cancellationToken));

    [HttpPost("units/{unitId:guid}/lessons")]
    [ProducesResponseType<ApiResponse<TeacherCourseDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TeacherCourseDto>>> CreateLesson(
        Guid unitId, LessonRequest request, CancellationToken cancellationToken) =>
        FromResult(await content.CreateLessonAsync(unitId, request, cancellationToken), StatusCodes.Status201Created);
}
