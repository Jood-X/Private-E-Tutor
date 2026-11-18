using Microsoft.AspNetCore.Identity;

namespace ETutor.Infrastructure.Identity;

/// <summary>
/// The credential record (email, password hash, lockout, roles). Business data lives in
/// <see cref="ETutor.Domain.Users.UserProfile"/>, which shares this ID.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>;
