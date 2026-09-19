using MudBlazor;

namespace AshaNandanvan.Web.Components.Layout;

public static class ThemeFactory
{
    public static MudTheme Create() => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#b6e34a",
            Secondary = "#8bc41f",
            Tertiary = "#1c3d24",
            Background = "#070807",
            Surface = "#141814",
            AppbarBackground = "#070807",
            AppbarText = "#f4f6f1",
            DrawerBackground = "#0c0f0c",
            TextPrimary = "#f4f6f1",
            TextSecondary = "#a7b3a4"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Manrope", "Segoe UI", "sans-serif"]
            },
            H1 = new H1Typography { FontFamily = ["Oswald", "Impact", "sans-serif"] },
            H2 = new H2Typography { FontFamily = ["Oswald", "Impact", "sans-serif"] },
            H3 = new H3Typography { FontFamily = ["Oswald", "Impact", "sans-serif"] },
            H4 = new H4Typography { FontFamily = ["Oswald", "Impact", "sans-serif"] },
            H5 = new H5Typography { FontFamily = ["Oswald", "Impact", "sans-serif"] },
            H6 = new H6Typography { FontFamily = ["Oswald", "Impact", "sans-serif"] }
        }
    };
}
