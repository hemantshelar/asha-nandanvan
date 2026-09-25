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

### Docker (recommended)

Keep two files and do not mix them:

| File | What goes here |
|---|---|
| `src/AshaNandanvan.Web/secrets.json` | Google, Square, admin seed email (same keys as Azure App Settings) |
| `.env` | SQL container password, database name, and host SQL settings |

The **https (Docker SQL)** profile only sets `ASHA_SQL_SOURCE=docker`. That builds `ConnectionStrings__DefaultConnection` from `.env` and leaves `secrets.json` alone. The default **https** profile still uses LocalDB from `appsettings.json`.

Azure uses the same setting names; only the SQL auth value changes (`sa` locally, Entra / managed identity in Azure).

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/) and the .NET 10 SDK.
2. From the repo root:

```powershell
.\infra\scripts\ensure-local-config.ps1
```

3. Put Google / Square / admin values in `src/AshaNandanvan.Web/secrets.json`. Put the SQL password in `.env`. Do not commit either file.
4. Pick a workflow:

**A — Debug the app on the host, SQL in Docker**

```powershell
docker compose up sql -d
dotnet run --project src/AshaNandanvan.Web --launch-profile "https (Docker SQL)"
```

In Cursor use **https (Docker SQL)** (starts SQL, then F5). Google / Square still come from `secrets.json`.

Open https://localhost:7095

**B — Test the published site in a container (closest to Azure)**

```powershell
docker compose up --build
```

Open http://localhost:8080  
Google redirect URI for this mode: `http://localhost:8080/signin-google`

**C — Debug inside the web container (hot reload)**

```powershell
docker compose --profile debug up --build
```

Then **Attach to Docker web-debug**. The site is http://localhost:8080.

Stop everything with `docker compose --profile debug down`. SQL data stays in the `sql-data` volume.

On first start the app applies EF migrations and seeds sample products.

### Without Docker

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

1. `appsettings.json` — committed defaults (LocalDB)
2. `appsettings.{Environment}.json` — e.g. `appsettings.Development.json` (optional)
3. `secrets.json` in the Web project folder, then .NET User Secrets (Google, Square, admin)
4. Docker SQL connection from `.env` — only when `ASHA_SQL_SOURCE=docker`
5. Environment variables (and command-line args last)

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

Google authorized redirect URIs:

- Host debug: `https://localhost:7095/signin-google`
- Docker web / debug profile: `http://localhost:8080/signin-google`
- Azure: `https://app-ashanandanvan-dev.azurewebsites.net/signin-google`

If you change `MSSQL_SA_PASSWORD` in `.env`, update the **https (Docker SQL)** launch profile or run `export-docker-env.ps1` so the host uses the same password.

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
| SQL database | `ashanandanvan-dev` on existing server `invtation.database.windows.net` |

The SQL **server** is not created. The new database stays in `invtation-web_group` because that is where the server lives.

F1 cannot bind `ashanandanvan.com.au` or keep the site always on. Use `https://app-ashanandanvan-dev.azurewebsites.net` until you move to Basic (B1).

### One-time Azure + GitHub setup

1. `az login`
2. Run `infra/scripts/setup-github-oidc.ps1`
3. In GitHub: **Settings → Environments → New environment → `dev`**
4. Add the **environment secrets** the script prints on `dev` (Azure IDs, SQL admin group, Google, Square, admin email)
5. In Google Cloud, add `https://app-ashanandanvan-dev.azurewebsites.net/signin-google`

The script creates Entra group `ashanandanvan-sql-admins-dev` (you + the GitHub app) and Bicep sets that group as the SQL Entra admin so the pipeline can grant the web app's managed identity `db_owner`.
