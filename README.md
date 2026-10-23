# Inventory Management System

**Purchasing, warehouse stock, and supplier visibility — one connected workflow.**

![Angular](https://img.shields.io/badge/Angular-20-DD0031)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![SQL Server](https://img.shields.io/badge/SQL_Server-Database-CC2927)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Email_Queue-FF6600)

A full-stack inventory app with purchasing, a supplier portal, department stock issues, barcode lookup, queued email, company-based access, and an AI-assisted inventory dashboard.

## Highlights

| Feature | What it does |
| --- | --- |
| Inventory | Products, departments, warehouses, and units of measure |
| Purchasing | Purchase orders, approval, and goods receipts |
| Purchase returns | Combine items from multiple GRNs; deduct stock when posted |
| Supplier portal | Read-only PO, GRN, and return lists with search, date filters, pagination, details, and printing |
| Supplier accounts | Manage supplier details and optional login accounts from User Management |
| Amounts | Snapshot GRN/return prices, MMK totals, and a supplier dashboard showing received value minus posted returns |
| Stock issues | Issue items to departments; barcode/SKU entry, Draft → Posted workflow, and stock deduction |
| Barcodes | Generate product barcodes and look up items with a scanner or SKU |
| Queued email | Purchase-order emails through RabbitMQ, a database outbox, and SMTP |
| Authentication | HttpOnly JWT cookies, refresh tokens, CSRF protection, and separate company/supplier permissions |
| Company accounts | Admin company selection and company-scoped inventory data |
| AI dashboard | Groq-generated summaries and validated card/table/chart definitions, using authenticated inventory facts through MCP |
| Dashboard charts | Bar, line, area, stacked bar, pie, and doughnut views; date filters, English/Myanmar requests, and a standard view when AI is unavailable |
| Unit display | UOM symbols in dashboard data, product lists, packaging labels, and stock movements |
| Documents & UI | PO/GRN/return printing, Excel export, and light/dark themes |

```text
Purchase Order → Approval → Goods Receipt → Warehouse Stock
                                                ├─ Purchase Return → Supplier
                                                └─ Stock Issue → Department
```

Returns and issues change stock when posted. Drafts support editing and soft deletion; posted records are protected from changes.

Supplier accounts share the existing layout and see only their own documents. Net received value excludes invoices and payments.

## Tech & skills

- **Frontend:** Angular 20 · TypeScript · PrimeNG · Chart.js · Tailwind CSS · RxJS
- **Backend:** ASP.NET Core 10 · C# · EF Core · SQL Server
- **Patterns:** Layered architecture · CQRS with MediatR · FluentValidation
- **Integrations:** Groq tool calling · Model Context Protocol (MCP) · RabbitMQ · MailKit/SMTP · Barcode generation & lookup · Excel export

## Run locally

Requires .NET 10, Node.js, npm, and SQL Server.

Set the database connection and administrator credentials from the repository root. Replace the example values with your own:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=InventoryManagement;Trusted_Connection=True;TrustServerCertificate=True" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "JwtSettings:SecretKey" "replace-with-your-own-random-secret-at-least-32-characters" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "DefaultAdmin:UserName" "localadmin" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "DefaultAdmin:Email" "localadmin@example.com" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "DefaultAdmin:Password" "replace-with-your-own-strong-password" --project src/InventoryManagementSystem.Api
```

On a fresh Development database, the API applies migrations and creates the configured administrator.

Start the API:

```powershell
dotnet dev-certs https --trust
dotnet run --project src/InventoryManagementSystem.Api --launch-profile https
```

Start the client in another terminal:

```bash
cd src/InventoryManagementSystem.Client
npm ci
npm start
```

Open http://localhost:4200 and sign in with the credentials above. API docs are at https://localhost:7152/docs/scalar.

### Optional: purchase-order email

```powershell
docker compose -f compose.email.yml up -d
dotnet user-secrets set "RabbitMqEmail:Enabled" "true" --project src/InventoryManagementSystem.Api
```

Configure `SmtpSettings` through API user secrets, then restart the API. The email worker is disabled by default in Development.

### Optional: AI inventory dashboard

Configure a Groq API key and a model that supports local function calling. Keep the key in API user secrets; the Angular client never needs it:

```powershell
dotnet user-secrets set "Ai:Groq:ApiKey" "replace-with-your-groq-api-key" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "Ai:Groq:Model" "replace-with-your-tool-calling-model-id" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "Ai:Mcp:Endpoint" "https://localhost:7152/api/process/mcp" --project src/InventoryManagementSystem.Api
```

Restart the API, sign in with an internal account, select a company, and open **Dashboard**. Choose the language and dates, then select **Refresh data**. Date ranges include both endpoints and are limited to 90 days.

The configured MCP endpoint must be a trusted HTTPS address: the backend forwards the authenticated user's access token to it. Development defaults to the address above; other environments require an explicit `Ai:Mcp:Endpoint` setting.

```text
Angular → Dashboard API → authenticated MCP facts tool → inventory query
                       → Groq tool call → MCP evidence → Groq dashboard JSON
                       → validation + definition cache → Angular chart renderer
```

The API hosts one read-only MCP tool, `GetDashboardFacts`. The model selects supported widgets and writes summaries, titles, descriptions, and labels; displayed quantities come from database facts. Company scope comes from the authenticated user, not model arguments. MCP uses Bearer authentication while the browser retains the existing cookie/CSRF flow.

Stock and approved open orders are current snapshots. The date filter applies to posted departmental usage, which is **not sales**. Missing usage records are not treated as zero demand. Charts validate product IDs, compatible units, and the observations required by each chart type; UOM symbols are used for display.

Validated definitions are cached for five minutes using company, language, dates, and actual facts. A normal cache miss uses two Groq requests; a short final-turn rate limit permits one retry. Rate limits trigger a cooldown, and the dashboard attempts a direct facts query for its standard view when generation fails. Provider quota and input limits still apply. English/Myanmar generation follows the language switch, but the model's wording and factual prose can still need review.

This implementation does not yet calculate order quantities from supplier lead times or pending purchase quantities, forecast demand, or create purchase orders automatically. See [AI dashboard and MCP implementation notes](docs/ai-dashboard-mcp.md) for the contracts, validation rules, error handling, and verification details.

### Local demonstration data

[Local demo scripts and notes](scripts/sql/realistic-demo.md) document the explicitly created fictional purchasing, stock, and master-data batches. These scripts are tied to the local `InventoryManagement` database, company 1 (`TestCom`), existing records, and the saved row manifest; they are not a general fresh-database seed or an application startup step. The manifest contains demo record identifiers, not a database backup. Review the prerequisites before using the scripts on a matching local database.

Build output, DLLs, local previews, and verification artifacts under `output/` are excluded from version control. API keys and local database credentials belong in user secrets or deployment configuration.
