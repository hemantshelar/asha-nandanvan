# Asha Nandanvan

A .NET 10 Blazor site for the [Asha Nandanvan](https://www.facebook.com/profile.php?id=61587331501473) backyard in Sydney: sustainable living, worm composting, chickens, and a pickup shop for eggs, vegetables, live worms, and black gold compost.

## Stack

- .NET 10 Blazor Web App (Interactive Server)
- MudBlazor 9 with a custom earthy theme and a **top horizontal menu** (no left sidebar)
- EF Core + SQL Server LocalDB
- ASP.NET Core Identity + Google sign-in
- IOptions for store, Google, payment, and admin settings
- `IPaymentProvider` — Square sandbox checkout when keys are present; Stripe or a local mock as fallback

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

The host loads every source that exists. **A later source wins** for the same key:

1. `appsettings.json` — committed defaults
2. `appsettings.{Environment}.json` — e.g. `appsettings.Development.json` (optional)
3. `secrets.json` in the Web project folder, then .NET User Secrets (`%APPDATA%\Microsoft\UserSecrets\...`) (optional)
4. Environment variables (and command-line args last)

Missing files are skipped. `IOptions<T>` reads the merged result.

Examples:

```powershell
$env:GoogleAuth__ClientId = "..."
$env:ConnectionStrings__DefaultConnection = "Server=..."
```

Set local secrets (do not commit real keys):

```powershell
cd src/AshaNandanvan.Web
dotnet user-secrets init
dotnet user-secrets set "GoogleAuth:ClientId" "..."
dotnet user-secrets set "GoogleAuth:ClientSecret" "..."
dotnet user-secrets set "Admin:SeedEmail" "your.google.account@gmail.com"
dotnet user-secrets set "Payment:Provider" "Square"
dotnet user-secrets set "Payment:Square:ApplicationId" "sandbox-sq0idb-..."
dotnet user-secrets set "Payment:Square:AccessToken" "EAAA..."
dotnet user-secrets set "Payment:Square:LocationId" "L..."
dotnet user-secrets set "Payment:Square:UseSandbox" "true"
dotnet user-secrets set "Payment:Square:WebhookSignatureKey" ""
```

Google authorized redirect URI: `https://localhost:7095/signin-google`

The first Google account that matches `Admin:SeedEmail` is promoted to Admin and can manage products and orders.

`Payment:Provider` is `Square`. Checkout redirects to Square hosted checkout. After you pay, Square returns to `/checkout/confirmation/{orderNumber}` and the app marks the order paid. Localhost cannot receive Square webhooks, so that return confirmation is the paid path.

Square sandbox test card: `4111 1111 1111 1111`, any future expiry, any CVV, any postcode.

## CI/CD (GitHub Actions + Azure)

Pushes to `feature/002-squre-pay` build the site, upload a web artifact, deploy Bicep to Azure, then zip-deploy the app. Pull requests only build.

| Azure resource | Name |
|---|---|
| Resource group | `ashanandanvan-dev` |
| App Service plan | `plan-ashanandanvan-dev` (F1 Free, Linux) |
| Web app | `app-ashanandanvan-dev` |
| Application Insights | `appi-ashanandanvan-dev` |
| Log Analytics | `law-ashanandanvan-dev` |
| SQL database | `ashanandanvan-dev` on existing server `invitation.database.windows.net` |

The SQL **server** is not created. The new database stays in `invitation-web-group` because that is where the server lives.

F1 cannot bind `ashanandanvan.com.au` or keep the site always on. Use `https://app-ashanandanvan-dev.azurewebsites.net` until you move to Basic (B1).

### One-time Azure + GitHub setup

1. `az login`
2. Run `infra/scripts/setup-github-oidc.ps1`
3. In GitHub: **Settings → Environments → New environment → `dev`**
4. Add the variables and secrets the script prints (Azure IDs, SQL admin group, Google, Square, admin email)
5. In Google Cloud, add `https://app-ashanandanvan-dev.azurewebsites.net/signin-google`

The script creates Entra group `ashanandanvan-sql-admins-dev` (you + the GitHub app) and Bicep sets that group as the SQL Entra admin so the pipeline can grant the web app's managed identity `db_owner`.
