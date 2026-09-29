using AshaNandanvan.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AshaNandanvan.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public UserSignupSource SignupSource { get; set; } = UserSignupSource.Unknown;
    public DateTimeOffset SignedUpAt { get; set; }
    public bool IsBlocked { get; set; }
    public DateTimeOffset? BlockedAt { get; set; }
    public string? BlockedReason { get; set; }
}
