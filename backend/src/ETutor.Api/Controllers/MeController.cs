using ETutor.Api.Common;
using ETutor.Api.Security;
using ETutor.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Controllers;

/// <summary>The signed-in user's own profile. There is no user ID in these routes by design.</summary>
[Authorize]
[Route(ApiRoutes.V1 + "/me")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
public sealed class MeController(ProfileService profiles) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<MeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MeDto>>> Get(CancellationToken cancellationToken) =>
        FromResult(await profiles.GetMeAsync(cancellationToken));

    [HttpPut]
    [ProducesResponseType<ApiResponse<MeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MeDto>>> Update(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        FromResult(await profiles.UpdateProfileAsync(request, cancellationToken));

    [HttpPut("student")]
    [Authorize(Policy = Policies.Student)]
    [ProducesResponseType<ApiResponse<MeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<MeDto>>> UpdateStudy(
        UpdateStudentStudyRequest request, CancellationToken cancellationToken) =>
        FromResult(await profiles.UpdateStudentStudyAsync(request, cancellationToken));

    [HttpPut("teacher")]
    [Authorize(Policy = Policies.Teacher)]
    [ProducesResponseType<ApiResponse<MeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<MeDto>>> UpdateTeaching(
        UpdateTeacherProfileRequest request, CancellationToken cancellationToken) =>
        FromResult(await profiles.UpdateTeacherProfileAsync(request, cancellationToken));
}
