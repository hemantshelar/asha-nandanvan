using System.Security.Claims;
using AshaNandanvan.Application.Common;
using AshaNandanvan.Application.Options;
using AshaNandanvan.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AshaNandanvan.Web.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/account/google-login", (
            SignInManager<ApplicationUser> signInManager,
            IOptions<GoogleAuthOptions> google,
            string? returnUrl) =>
        {
            if (!google.Value.IsConfigured)
            {
                return Results.Redirect("/account/login?error=not-configured");
            }

            var safeReturn = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') ? "/" : returnUrl;
            var redirectUrl = $"/account/external-callback?returnUrl={Uri.EscapeDataString(safeReturn)}";
            var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Results.Challenge(properties, ["Google"]);
        });

        endpoints.MapGet("/account/external-callback", async (
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IOptions<AdminOptions> adminOptions,
            string? returnUrl) =>
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                return Results.Redirect("/account/login?error=google");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.Redirect("/account/login?error=email");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name)
                };

                var create = await userManager.CreateAsync(user);
                if (!create.Succeeded)
                {
                    return Results.Redirect("/account/login?error=create");
                }

                await userManager.AddToRoleAsync(user, AppRoles.Customer);
                if (IsSeedAdmin(email, adminOptions.Value.SeedEmail))
                {
                    await userManager.AddToRoleAsync(user, AppRoles.Admin);
                }
            }
            else if (IsSeedAdmin(email, adminOptions.Value.SeedEmail)
                     && !await userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                await userManager.AddToRoleAsync(user, AppRoles.Admin);
            }

            var existingLogin = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingLogin is null)
            {
                await userManager.AddLoginAsync(user, info);
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            var safeReturn = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') ? "/" : returnUrl;
            return Results.Redirect(safeReturn);
        });

        endpoints.MapGet("/account/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Redirect("/");
        });

        return endpoints;
    }

    private static bool IsSeedAdmin(string email, string seedEmail) =>
        !string.IsNullOrWhiteSpace(seedEmail)
        && email.Equals(seedEmail.Trim(), StringComparison.OrdinalIgnoreCase);
}
