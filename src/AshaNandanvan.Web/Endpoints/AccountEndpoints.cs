using System.Security.Claims;
using AshaNandanvan.Application.Analytics;
using AshaNandanvan.Application.Common;
using AshaNandanvan.Application.Options;
using AshaNandanvan.Application.Users;
using AshaNandanvan.Domain.Enums;
using AshaNandanvan.Infrastructure.Identity;
using AshaNandanvan.Web.Services;
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
            ChallengeExternal(signInManager, google.Value.IsConfigured, "Google", returnUrl));

        endpoints.MapGet("/account/facebook-login", (
            SignInManager<ApplicationUser> signInManager,
            IOptions<FacebookAuthOptions> facebook,
            string? returnUrl) =>
            ChallengeExternal(signInManager, facebook.Value.IsConfigured, "Facebook", returnUrl));

        endpoints.MapGet("/account/external-callback", async (
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IOptions<AdminOptions> adminOptions,
            IVisitStore visits,
            HttpContext http,
            string? returnUrl) =>
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                return Results.Redirect("/account/login?error=external");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Results.Redirect("/account/login?error=email");
            }

            var user = await userManager.FindByEmailAsync(email);
            var isNewAccount = user is null;
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = info.Principal.FindFirstValue(ClaimTypes.Name),
                    SignupSource = UserSignup.FromReturnUrl(returnUrl),
                    SignedUpAt = DateTimeOffset.UtcNow
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

            if (user.IsBlocked)
            {
                return Results.Redirect("/account/login?error=blocked");
            }

            var existingLogin = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingLogin is null)
            {
                await userManager.AddLoginAsync(user, info);
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            await RecordArrivalAsync(visits, http, user.Id, isNewAccount, info.LoginProvider);

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

    private static IResult ChallengeExternal(
        SignInManager<ApplicationUser> signInManager,
        bool configured,
        string provider,
        string? returnUrl)
    {
        if (!configured)
        {
            return Results.Redirect("/account/login?error=not-configured");
        }

        var safeReturn = string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') ? "/" : returnUrl;
        var redirectUrl = $"/account/external-callback?returnUrl={Uri.EscapeDataString(safeReturn)}";
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Results.Challenge(properties, [provider]);
    }

    /// <summary>
    /// Ties the sign-in back to the visit it came from, which is what lets the insights page
    /// say "Gumtree sent eleven visitors and two of them registered".
    /// </summary>
    private static async Task RecordArrivalAsync(
        IVisitStore visits,
        HttpContext http,
        string userId,
        bool isNewAccount,
        string provider)
    {
        var sessionKey = VisitCookies.ReadGuid(http, VisitCookies.Session);
        if (sessionKey is null)
        {
            return;
        }

        try
        {
            var sessionId = await visits.FindBySessionKeyAsync(sessionKey.Value);
            if (sessionId is not int id)
            {
                return;
            }

            await visits.RecordEventAsync(
                id,
                isNewAccount ? VisitEventKind.Registered : VisitEventKind.SignedIn,
                "/account/external-callback",
                provider,
                userId: userId);
        }
        catch (Exception)
        {
            // Sign-in must succeed whether or not we managed to log it.
        }
    }

    private static bool IsSeedAdmin(string email, string seedEmail) =>
        !string.IsNullOrWhiteSpace(seedEmail)
        && email.Equals(seedEmail.Trim(), StringComparison.OrdinalIgnoreCase);
}
