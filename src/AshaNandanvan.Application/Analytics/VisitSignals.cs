using System.Text.RegularExpressions;
using AshaNandanvan.Domain.Enums;

namespace AshaNandanvan.Application.Analytics;

/// <summary>Low-entropy client hints. Chromium sends the first and last by default.</summary>
public sealed record ClientHints(
    string? Platform = null,
    string? PlatformVersion = null,
    string? Model = null,
    bool? Mobile = null);

public sealed record VisitAgent(
    VisitDevice Device,
    string Browser,
    string BrowserVersion,
    string Platform,
    string PlatformVersion,
    string? Model,
    bool IsBot);

/// <summary>
/// Turns raw request headers into the handful of buckets the insights page reports on.
/// Everything here is deliberately coarse: we want comparable groups, not fingerprints.
/// </summary>
public static class VisitSignals
{
    public const string Direct = "Direct";
    public const string Unknown = "Unknown";

    private static readonly Regex BotPattern = new(
        @"bot|crawl|spider|slurp|scrape|curl|wget|python-requests|httpclient|go-http|java/|okhttp|axios|headless|phantomjs|lighthouse|pingdom|uptime|monitor|statuscake|semrush|ahrefs|mj12|dotbot|petalbot|yandex|baidu|facebookexternalhit|whatsapp|telegram|preview|validator|feedfetcher|archiver",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GuidLike = new(
        @"^[0-9a-f]{8}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{4}-?[0-9a-f]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<string, string> ShortCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["fb"] = "Facebook",
        ["facebook"] = "Facebook",
        ["ig"] = "Instagram",
        ["insta"] = "Instagram",
        ["instagram"] = "Instagram",
        ["yt"] = "YouTube",
        ["youtube"] = "YouTube",
        ["gt"] = "Gumtree",
        ["gum"] = "Gumtree",
        ["gumtree"] = "Gumtree",
        ["wa"] = "WhatsApp",
        ["whatsapp"] = "WhatsApp",
        ["em"] = "Email",
        ["email"] = "Email",
        ["qr"] = "QR code",
        ["fl"] = "Flyer",
        ["flyer"] = "Flyer",
        ["tt"] = "TikTok",
        ["tiktok"] = "TikTok",
        ["li"] = "LinkedIn",
        ["linkedin"] = "LinkedIn",
        ["nd"] = "Nextdoor",
        ["nextdoor"] = "Nextdoor",
        ["wom"] = "Word of mouth",
        ["card"] = "Business card",
        ["market"] = "Markets"
    };

    private static readonly (string Fragment, string Label)[] ReferrerHosts =
    [
        ("facebook.", "Facebook"),
        ("fb.me", "Facebook"),
        ("fb.com", "Facebook"),
        ("instagram.", "Instagram"),
        ("youtube.", "YouTube"),
        ("youtu.be", "YouTube"),
        ("gumtree.", "Gumtree"),
        ("google.", "Google"),
        ("bing.", "Bing"),
        ("duckduckgo.", "DuckDuckGo"),
        ("ecosia.", "Search"),
        ("yahoo.", "Search"),
        ("t.co", "X"),
        ("twitter.", "X"),
        ("x.com", "X"),
        ("linkedin.", "LinkedIn"),
        ("lnkd.in", "LinkedIn"),
        ("tiktok.", "TikTok"),
        ("nextdoor.", "Nextdoor"),
        ("reddit.", "Reddit"),
        ("pinterest.", "Pinterest"),
        ("outlook.", "Email"),
        ("mail.", "Email")
    ];

    public static bool LooksLikeBot(string? userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) || BotPattern.IsMatch(userAgent);

    public static VisitAgent Describe(string? userAgent, ClientHints? hints = null)
    {
        hints ??= new ClientHints();
        var ua = userAgent ?? string.Empty;
        var (browser, browserVersion) = ReadBrowser(ua);
        var (platform, platformVersion) = ReadPlatform(ua, hints);

        return new VisitAgent(
            ReadDevice(ua, hints),
            browser,
            browserVersion,
            platform,
            platformVersion,
            Trim(hints.Model, 80),
            LooksLikeBot(userAgent));
    }

    private static VisitDevice ReadDevice(string ua, ClientHints hints)
    {
        if (ua.Length == 0)
        {
            return VisitDevice.Unknown;
        }

        if (Has(ua, "ipad") || Has(ua, "tablet") || Has(ua, "kindle") || Has(ua, "playbook")
            || (Has(ua, "android") && !Has(ua, "mobile")))
        {
            return VisitDevice.Tablet;
        }

        if (hints.Mobile == true || Has(ua, "mobile") || Has(ua, "iphone") || Has(ua, "ipod")
            || Has(ua, "android") || Has(ua, "windows phone"))
        {
            return VisitDevice.Mobile;
        }

        return VisitDevice.Desktop;
    }

    private static (string Browser, string Version) ReadBrowser(string ua)
    {
        // Order matters: Chromium forks all keep "Chrome" and "Safari" in their strings.
        if (Has(ua, "edg/") || Has(ua, "edgios/") || Has(ua, "edga/"))
        {
            return ("Edge", FirstVersion(ua, "edg/", "edgios/", "edga/"));
        }

        if (Has(ua, "opr/") || Has(ua, "opera"))
        {
            return ("Opera", FirstVersion(ua, "opr/", "opera/"));
        }

        if (Has(ua, "samsungbrowser/"))
        {
            return ("Samsung Internet", FirstVersion(ua, "samsungbrowser/"));
        }

        if (Has(ua, "fxios/") || Has(ua, "firefox/"))
        {
            return ("Firefox", FirstVersion(ua, "fxios/", "firefox/"));
        }

        if (Has(ua, "crios/"))
        {
            return ("Chrome", FirstVersion(ua, "crios/"));
        }

        if (Has(ua, "chrome/"))
        {
            return ("Chrome", FirstVersion(ua, "chrome/"));
        }

        if (Has(ua, "safari/") && Has(ua, "version/"))
        {
            return ("Safari", FirstVersion(ua, "version/"));
        }

        if (Has(ua, "safari/"))
        {
            return ("Safari", string.Empty);
        }

        return (Unknown, string.Empty);
    }

    private static (string Platform, string Version) ReadPlatform(string ua, ClientHints hints)
    {
        var hinted = Clean(hints.Platform);
        var hintedVersion = Clean(hints.PlatformVersion);

        if (hinted.Equals("Windows", StringComparison.OrdinalIgnoreCase))
        {
            // Windows 11 is only distinguishable through the client hint: its user-agent
            // string still says "Windows NT 10.0".
            return ("Windows", WindowsName(hintedVersion));
        }

        if (Has(ua, "windows nt"))
        {
            var nt = VersionAfter(ua, "windows nt ");
            return ("Windows", nt switch
            {
                "10" or "10.0" => "10 or 11",
                "6.3" => "8.1",
                "6.2" => "8",
                "6.1" => "7",
                _ => nt
            });
        }

        if (Has(ua, "iphone os") || Has(ua, "cpu os"))
        {
            var raw = Has(ua, "iphone os") ? VersionAfter(ua, "iphone os ") : VersionAfter(ua, "cpu os ");
            return ("iOS", raw.Replace('_', '.'));
        }

        if (Has(ua, "android"))
        {
            return ("Android", Prefer(hintedVersion, VersionAfter(ua, "android ")));
        }

        if (Has(ua, "mac os x"))
        {
            return ("macOS", Prefer(hintedVersion, VersionAfter(ua, "mac os x ").Replace('_', '.')));
        }

        if (Has(ua, "cros"))
        {
            return ("ChromeOS", hintedVersion);
        }

        if (Has(ua, "linux"))
        {
            return ("Linux", string.Empty);
        }

        return (hinted.Length > 0 ? hinted : Unknown, hintedVersion);
    }

    private static string WindowsName(string platformVersion)
    {
        var major = platformVersion.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!int.TryParse(major, out var value))
        {
            return "10 or 11";
        }

        return value >= 13 ? "11" : value > 0 ? "10" : "8.1 or older";
    }

    /// <summary>Reads a tagged source such as <c>?s=gt</c> or <c>?utm_source=gumtree</c>.</summary>
    public static string? FromTag(string? tag)
    {
        var value = Clean(tag);
        if (value.Length == 0)
        {
            return null;
        }

        return ShortCodes.TryGetValue(value, out var known)
            ? known
            : Capitalise(Trim(value, 40)!);
    }

    public static string FromReferrer(string? referrer, string? ownHost)
    {
        if (!Uri.TryCreate(Clean(referrer), UriKind.Absolute, out var uri))
        {
            return Direct;
        }

        var host = uri.Host.ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(ownHost) && host.Equals(ownHost, StringComparison.OrdinalIgnoreCase))
        {
            return Direct;
        }

        foreach (var (fragment, label) in ReferrerHosts)
        {
            if (host.Contains(fragment, StringComparison.Ordinal))
            {
                return label;
            }
        }

        return Capitalise(Trim(host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host, 40)!);
    }

    /// <summary>
    /// Drops the query string and anything that looks like an invite token, so page
    /// reports group sensibly and we are not storing secrets in the analytics tables.
    /// </summary>
    public static string NormalisePath(string? path)
    {
        var value = Clean(path);
        if (value.Length == 0)
        {
            return "/";
        }

        var cut = value.IndexOfAny(['?', '#']);
        if (cut >= 0)
        {
            value = value[..cut];
        }

        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        value = value.ToLowerInvariant();
        if (value.Length > 1)
        {
            value = value.TrimEnd('/');
        }

        var segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return "/";
        }

        for (var i = 0; i < segments.Length; i++)
        {
            if (GuidLike.IsMatch(segments[i]) || segments[i].Length > 24)
            {
                segments[i] = "token";
            }
        }

        return Trim("/" + string.Join('/', segments), 300)!;
    }

    public static string DeviceLabel(VisitDevice device) => device switch
    {
        VisitDevice.Desktop => "Desktop",
        VisitDevice.Mobile => "Mobile",
        VisitDevice.Tablet => "Tablet",
        _ => Unknown
    };

    public static string EventLabel(VisitEventKind kind) => kind switch
    {
        VisitEventKind.PageView => "Page view",
        VisitEventKind.Registered => "Registered",
        VisitEventKind.SignedIn => "Signed in",
        VisitEventKind.AddedToCart => "Added to basket",
        VisitEventKind.CheckoutOpened => "Opened checkout",
        VisitEventKind.OrderPlaced => "Placed an order",
        VisitEventKind.BookingBlockedPending => "Blocked by pending order",
        VisitEventKind.BookingFull => "Dates already full",
        VisitEventKind.PaymentOpened => "Opened payment",
        _ => kind.ToString()
    };

    private static bool Has(string ua, string token) =>
        ua.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static string FirstVersion(string ua, params string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (Has(ua, token))
            {
                return VersionAfter(ua, token);
            }
        }

        return string.Empty;
    }

    /// <summary>Keeps at most two version segments; "125.0.6422.1" becomes "125".</summary>
    private static string VersionAfter(string ua, string token)
    {
        var index = ua.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return string.Empty;
        }

        var start = index + token.Length;
        var end = start;
        while (end < ua.Length && (char.IsAsciiDigit(ua[end]) || ua[end] is '.' or '_'))
        {
            end++;
        }

        var raw = ua[start..end].Replace('_', '.').Trim('.');
        if (raw.Length == 0)
        {
            return string.Empty;
        }

        var parts = raw.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var kept = parts.Length > 1 && parts[1] != "0"
            ? $"{parts[0]}.{parts[1]}"
            : parts[0];

        return Trim(kept, 40)!;
    }

    private static string Prefer(string first, string second) => first.Length > 0 ? first : second;

    private static string Clean(string? value) => (value ?? string.Empty).Trim().Trim('"');

    private static string Capitalise(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
