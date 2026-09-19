# Asha Nandanvan

A .NET 10 Blazor site for the [Asha Nandanvan](https://www.facebook.com/profile.php?id=61587331501473) backyard in Sydney: sustainable living, worm composting, chickens, and a pickup shop for eggs, vegetables, live worms, and black gold compost.

## Stack

- .NET 10 Blazor Web App (Interactive Server)
- MudBlazor 9 with a custom earthy theme and a **top horizontal menu** (no left sidebar)
- EF Core + SQL Server LocalDB
- ASP.NET Core Identity + Google sign-in
- IOptions for store, Google, payment, and admin settings
- `IPaymentProvider` — Stripe when keys are present, otherwise a local mock so checkout can still be demonstrated

## Run locally

1. Install the .NET 10 SDK and SQL Server LocalDB.
2. From the repo root:

```powershell
dotnet ef database update --project src/AshaNandanvan.Infrastructure --startup-project src/AshaNandanvan.Web
dotnet run --project src/AshaNandanvan.Web
```

Open https://localhost:7095

On first run the app also applies migrations and seeds sample products.

## Configuration (`IOptions`)

Set these in user secrets or `appsettings.Development.json` (do not commit real keys):

```powershell
cd src/AshaNandanvan.Web
dotnet user-secrets init
dotnet user-secrets set "GoogleAuth:ClientId" "..."
dotnet user-secrets set "GoogleAuth:ClientSecret" "..."
dotnet user-secrets set "Admin:SeedEmail" "your.google.account@gmail.com"
dotnet user-secrets set "Payment:Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Payment:Stripe:PublishableKey" "pk_test_..."
dotnet user-secrets set "Payment:Stripe:WebhookSecret" "whsec_..."
```

Google authorized redirect URI: `https://localhost:7095/signin-google`

The first Google account that matches `Admin:SeedEmail` is promoted to Admin and can manage products and orders.

Without Stripe keys, checkout uses the mock provider and marks the order paid immediately.

## Payment providers

`Payment:Provider` is `Stripe` today. The shop talks only to `IPaymentProvider`, so Square can be added later without changing the checkout page.
