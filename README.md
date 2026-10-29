# Inventory Management System

**Purchasing, warehouse stock, and supplier visibility — one connected workflow.**

![Angular](https://img.shields.io/badge/Angular-20-DD0031)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![SQL Server](https://img.shields.io/badge/SQL_Server-Database-CC2927)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Email_Queue-FF6600)
![Redis](https://img.shields.io/badge/Redis-Dashboard_Cache-DC382D)

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
| Redis cache & coordination | Reuse validated dashboard definitions, coordinate generation across API instances, and share provider rate-limit cooldowns |
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
- **Integrations:** Groq tool calling · Model Context Protocol (MCP) · Redis with StackExchange.Redis · RabbitMQ · MailKit/SMTP · Barcode generation & lookup · Excel export

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
dotnet user-secrets set "ConnectionStrings:Redis" "127.0.0.1:6379,abortConnect=false" --project src/InventoryManagementSystem.Api
```

Start Docker Desktop and the local Redis cache:

```powershell
docker compose -p inventory-cache -f compose.cache.yml up -d
docker compose -p inventory-cache -f compose.cache.yml exec redis redis-cli ping
```

The second command should return `PONG`. This Compose setup publishes Redis only on localhost, limits Redis data memory to 128 MB, and uses `noeviction`: writes that need more memory fail rather than evicting coordination keys. Persistence is disabled, so restarting Redis clears cached definitions, cooldowns, and leases. The connection above assumes the API runs on the host.

Restart the API, sign in with an internal account, select a company, and open **Dashboard**. Choose the language and dates, then select **Refresh data**. Date ranges include both endpoints and are limited to 90 days.

The configured MCP endpoint must be a trusted HTTPS address: the backend forwards the authenticated user's access token to it. Development defaults to the address above; other environments require an explicit `Ai:Mcp:Endpoint` setting.

```text
Angular → Dashboard API → authenticated MCP facts tool → inventory query
                       → Groq tool call → MCP evidence → Groq dashboard JSON
                       → validation + definition cache → Angular chart renderer
```

The API hosts one read-only MCP tool, `GetDashboardFacts`. The model selects supported widgets and writes summaries, titles, descriptions, and labels; displayed quantities come from database facts. Company scope comes from the authenticated user, not model arguments. MCP uses Bearer authentication while the browser retains the existing cookie/CSRF flow.

Stock and approved open orders are current snapshots. The date filter applies to posted departmental usage, which is **not sales**. Missing usage records are not treated as zero demand. Charts validate product IDs, compatible units, and the observations required by each chart type; UOM symbols are used for display.

English/Myanmar generation follows the language switch, but the model's wording and factual prose can still need review. Provider quota and input limits still apply.

This implementation does not yet calculate order quantities from supplier lead times or pending purchase quantities, forecast demand, or create purchase orders automatically. See [AI dashboard and MCP implementation notes](docs/ai-dashboard-mcp.md) for the contracts, validation rules, error handling, and verification details.

#### Dashboard cache and provider coordination

The cache stores validated **dashboard definitions**. Current inventory facts are still read from the database; cached UI definitions do not authorize stock changes or replace transactional balance checks.

| Mechanism | Behavior |
| --- | --- |
| Definition lookup | Local memory first, then Redis; generate only after a cache miss and coordination checks |
| Cache identity | Company, language, selected dates, generation version, and actual facts determine the fingerprint |
| Lifetime | Five-minute definition TTL; changes in fingerprinted facts use a different key |
| Shared generation lease | One provider generation at a time per Redis database/environment; a 30-second lease renews every 10 seconds |
| Ownership | Owner tokens guard renewal, release, and publication; lease loss cancels generation |
| Shared cooldown | Provider `429` responses publish a wait visible to other API instances; shorter waits cannot reduce a longer remaining cooldown |
| Cached responses | Valid cached definitions remain available during cooldown without calling Groq |
| Coordination outage | Cold generation returns `503`; the client attempts its direct facts fallback, while warm memory hits remain available |

API instances must use the same Redis database and environment name to share coordination. Local and shared lock acquisition each have a 30-second wait limit; generation receives a four-minute cancellation budget. Coordination targets the standalone Redis service in this setup; Redis restart clears its metadata.

A normal successful cache miss uses two Groq requests. A final-turn `429` with a short Retry-After permits one final-request retry; the tool is not executed again by that retry. Cache hits make no Groq requests.

After a successful generation in Development, inspect definition keys:

```powershell
docker compose -p inventory-cache -f compose.cache.yml exec redis redis-cli --scan --pattern "inventory:Development:dashboard:definition:v1:*"
```

Copy one returned key and check its remaining lifetime:

```powershell
docker compose -p inventory-cache -f compose.cache.yml exec redis redis-cli TTL "paste-the-returned-key-here"
```

A newly stored definition has about 300 seconds remaining. `-2` means the key is absent or expired; `-1` means it has no expiry, which is unexpected for these definition keys. The API logs `Dashboard definition stored in Redis for five minutes.` on a successful write and `Dashboard definition cache hit in Redis.` on a Redis read. Same-instance requests may use memory instead, without a Redis-hit log. A failed AI generation does not create a definition entry.

### Dashboard data

The dashboard reads the selected company's existing records. Create products, receive goods, approve purchase orders, and post departmental stock issues through the normal workflows to provide stock, open-order, and usage evidence. Draft issues do not contribute to recorded usage. AI generation and MCP tool requests only read inventory data.

Build output, DLLs, local previews, and verification artifacts under `output/` are excluded from version control. API keys and local database credentials belong in user secrets or deployment configuration.
