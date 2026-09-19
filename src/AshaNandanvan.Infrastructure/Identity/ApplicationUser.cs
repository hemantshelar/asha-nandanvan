using Microsoft.AspNetCore.Identity;

namespace AshaNandanvan.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
