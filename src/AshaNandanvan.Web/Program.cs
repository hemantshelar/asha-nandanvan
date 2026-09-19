using System.Globalization;
using AshaNandanvan.Application;
using AshaNandanvan.Application.Cart;
using AshaNandanvan.Application.Common;
using AshaNandanvan.Application.Options;
using AshaNandanvan.Application.Storage;
using AshaNandanvan.Infrastructure;
using AshaNandanvan.Infrastructure.Data;
using AshaNandanvan.Infrastructure.Identity;
using AshaNandanvan.Web.Components;
using AshaNandanvan.Web.Endpoints;
using AshaNandanvan.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var culture = new CultureInfo("en-AU");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddMudServices();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IImageStorage, FileImageStorage>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/login";
});

var google = builder.Configuration.GetSection(GoogleAuthOptions.SectionName).Get<GoogleAuthOptions>();
if (google?.IsConfigured == true)
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = google.ClientId;
        options.ClientSecret = google.ClientSecret;
        options.CallbackPath = "/signin-google";
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppRoles.Admin, policy => policy.RequireRole(AppRoles.Admin));
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAccountEndpoints();
app.MapPaymentWebhooks();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
