using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Users;

public static class UserSignup
{
    public static UserSignupSource FromReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return UserSignupSource.Site;
        }

        var path = returnUrl.Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        var query = path.IndexOf('?', StringComparison.Ordinal);
        if (query >= 0)
        {
            path = path[..query];
        }

        return path.StartsWith("/albums/join/", StringComparison.OrdinalIgnoreCase)
            ? UserSignupSource.AlbumInvite
            : UserSignupSource.Site;
    }

    public static string SourceLabel(UserSignupSource source) => source switch
    {
        UserSignupSource.AlbumInvite => "Album invite",
        UserSignupSource.Site => "Website",
        _ => "Unknown"
    };

    public static string ActivityLabel(UserActivityKind activity) => activity switch
    {
        UserActivityKind.Admin => "Admin",
        UserActivityKind.HasStays => "Has stays",
        UserActivityKind.GuestOnly => "Guest only",
        _ => "No stays yet"
    };
}
