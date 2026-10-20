# Inventory Management System

**From purchase orders to warehouse stock — one connected workflow.**

![Angular](https://img.shields.io/badge/Angular-20-DD0031?logo=angular&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-Database-CC2927)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Email_Queue-FF6600?logo=rabbitmq&logoColor=white)

A full-stack inventory app with purchasing, barcode lookup, queued email, and company-based access.

## Highlights

| Feature | What it does |
| --- | --- |
| 📦 Inventory | Products, suppliers, departments, warehouses, and units of measure |
| 🛒 Purchasing | Purchase orders, approval, and goods receipts |
| ↩️ Purchase returns | Combine items from multiple GRNs; deduct stock when posted |
| 🔎 Barcodes | Generate product barcodes and look up items with a scanner or SKU |
| ✉️ Queued email | Purchase-order emails through RabbitMQ, a database outbox, and SMTP |
| 🔐 Authentication | Cookie-based JWT authentication, refresh tokens, roles, and CSRF protection |
| 🏢 Company accounts | Admin company selection and company-scoped inventory data |
| 🖨️ Documents & UI | PO/return printing, Excel export, and light/dark themes |

```text
Purchase Order → Approval → Goods Receipt → Warehouse Stock
                                  ↓
                           Purchase Return → Post → Stock Deduction
```

## Tech & skills

- **Frontend:** Angular 20 · TypeScript · PrimeNG · Tailwind CSS · RxJS
- **Backend:** ASP.NET Core 10 · C# · EF Core · SQL Server
- **Patterns:** Layered architecture · CQRS with MediatR · FluentValidation
- **Integrations:** RabbitMQ · MailKit/SMTP · Barcode generation & lookup · Excel export

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

## Feature guides

- [Purchase-order approval](docs/purchase-order-approval.md)
- [Purchase returns — မြန်မာ](docs/purchase-return-mm.md)
- [Company settings — မြန်မာ](docs/company-settings-mm.md)
- [Learning roadmap](docs/learning-roadmap.md)
