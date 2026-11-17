using ETutor.Api.Logging;
using ETutor.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Common;

/// <summary>
/// Base class for all API controllers. Controllers stay thin: call one service method,
/// then return <see cref="OkResponse{T}"/> or <see cref="FromResult{T}"/>.
/// </summary>
[ApiController]
[Produces("application/json")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status429TooManyRequests)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data) =>
        Ok(ApiResponse.Ok(data, HttpContext.GetTraceId()));

    protected ActionResult<ApiResponse<T>> FromResult<T>(Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsSuccess)
        {
            return OkResponse(result.Value);
        }

        var firstError = result.Errors[0];
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<ApiControllerBase>>();
        LogMessages.RequestFailed(logger, firstError.Code, firstError.Type);

        return StatusCode(
            ErrorStatusCodes.For(firstError.Type),
            ApiResponse.Fail<T>(ApiResponse.ToApiErrors(result.Errors), HttpContext.GetTraceId()));
    }
}
