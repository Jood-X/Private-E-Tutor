using ETutor.Application.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ETutor.Api.Security;

/// <summary>Reads the caller from the validated access token of the current HTTP request.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = accessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var subject = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(subject, out var id) ? id : null;
        }
    }

    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) == true;
}
