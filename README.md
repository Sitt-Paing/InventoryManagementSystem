# Inventory Management System

Inventory and purchasing app built with Angular, ASP.NET Core, and SQL Server.

Includes products, suppliers, warehouses, purchase-order approval, goods receipts, stock tracking, and purchase-order printing and email.

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

Purchase-order email needs RabbitMQ and SMTP settings. Start the local broker with `docker compose -f compose.email.yml up -d`, then configure `SmtpSettings` through API user secrets. The email worker is enabled in Development.
