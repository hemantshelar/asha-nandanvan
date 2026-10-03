using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace AshaNandanvan.Web.Services;

/// <summary>
/// The three first-party values a visit needs. None of them is a credential, so the browser
/// is allowed to read them: the Blazor circuit needs the session value after the page loads.
/// </summary>
public static class VisitCookies
{
    public const string Visitor = "an_v";
    public const string Session = "an_s";
    public const string FirstTouch = "an_fs";

    private static readonly TimeSpan VisitorLifetime = TimeSpan.FromDays(730);
    private static readonly TimeSpan FirstTouchLifetime = TimeSpan.FromDays(365);

    public static Guid? ReadGuid(HttpContext context, string name) =>
        context.Request.Cookies.TryGetValue(name, out var raw) && Guid.TryParse(raw, out var value)
            ? value
            : null;

    public static string? ReadText(HttpContext context, string name) =>
        context.Request.Cookies.TryGetValue(name, out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw
            : null;

    public static void WriteVisitor(HttpContext context, Guid visitorId) =>
        context.Response.Cookies.Append(Visitor, visitorId.ToString("N"), Options(context, VisitorLifetime));

    public static void WriteSession(HttpContext context, Guid sessionKey, TimeSpan lifetime) =>
        context.Response.Cookies.Append(Session, sessionKey.ToString("N"), Options(context, lifetime));

    public static void WriteFirstTouch(HttpContext context, string source) =>
        context.Response.Cookies.Append(FirstTouch, source, Options(context, FirstTouchLifetime));

    private static CookieOptions Options(HttpContext context, TimeSpan lifetime) => new()
    {
        HttpOnly = false,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Secure = context.Request.IsHttps,
        Path = "/",
        Expires = DateTimeOffset.UtcNow.Add(lifetime)
    };

    /// <summary>
    /// Hashes the address with a server-side salt so the stored value can count distinct
    /// networks but cannot be turned back into somebody's IP.
    /// </summary>
    public static (string Hash, string? Network) HashAddress(IPAddress? address, string salt)
    {
        if (address is null)
        {
            return (string.Empty, null);
        }

        var network = Network(address);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}|{address}"));
        return (Convert.ToHexString(bytes)[..32], network);
    }

    /// <summary>Coarse network label, kept for grouping rather than identification.</summary>
    private static string? Network(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var octets = address.GetAddressBytes();
            return $"{octets[0]}.{octets[1]}.{octets[2]}.0/24";
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var groups = address.GetAddressBytes();
            return string.Join(':',
            [
                $"{groups[0]:x2}{groups[1]:x2}",
                $"{groups[2]:x2}{groups[3]:x2}",
                $"{groups[4]:x2}{groups[5]:x2}",
                $"{groups[6]:x2}{groups[7]:x2}"
            ]) + "::/64";
        }

        return null;
    }
}
