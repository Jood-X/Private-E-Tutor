using ETutor.Api.Common;
using ETutor.Api.Configuration;
using ETutor.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ETutor.Api.Controllers;

/// <summary>
/// Registration, sign-in and token refresh. Rate-limited more strictly than other endpoints.
/// Access tokens last 15 minutes; refresh tokens are single-use and rotate on every refresh.
/// </summary>
[AllowAnonymous]
[EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
[Route(ApiRoutes.V1 + "/auth")]
public sealed class AuthController(RegistrationService registration, SessionService sessions) : ApiControllerBase
{
    [HttpPost("register/student")]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterStudent(
        RegisterStudentRequest request, CancellationToken cancellationToken) =>
        FromResult(await registration.RegisterStudentAsync(request, cancellationToken), StatusCodes.Status201Created);

    [HttpPost("register/teacher")]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterTeacher(
        RegisterTeacherRequest request, CancellationToken cancellationToken) =>
        FromResult(await registration.RegisterTeacherAsync(request, cancellationToken), StatusCodes.Status201Created);

    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        FromResult(await sessions.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    [ProducesResponseType<ApiResponse<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        FromResult(await sessions.RefreshAsync(request, cancellationToken));

    /// <summary>Signs out the session that owns the refresh token. Always 204, even for unknown tokens.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await sessions.LogoutAsync(request, cancellationToken);
        return NoContent();
    }
}
