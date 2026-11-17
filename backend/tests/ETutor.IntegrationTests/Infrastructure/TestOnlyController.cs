using System.ComponentModel.DataAnnotations;
using ETutor.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.IntegrationTests.Infrastructure;

/// <summary>Endpoints that exist only inside the test host to exercise cross-cutting behavior.</summary>
[Route("api/v1/test-only")]
public sealed class TestOnlyController : ApiControllerBase
{
    public const string SecretDetail = "SELECT * FROM Secrets WHERE Password = 'p@ss'";

    [HttpGet("throw")]
    public IActionResult Throw() => throw new InvalidOperationException(SecretDetail + Request.Path);

    [HttpPost("echo")]
    public ActionResult<ApiResponse<EchoRequest>> Echo(EchoRequest request) => OkResponse(request);
}

public sealed record EchoRequest([Required] string? Name, [Range(1, 10)] int Count);
