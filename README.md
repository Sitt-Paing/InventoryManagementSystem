<div align="center">

# 📦 Inventory Management System

### From purchasing to stock visibility — one connected workspace.

A full-stack inventory application built with **Angular**, **ASP.NET Core**, and **SQL Server**.
Manage product catalogs, suppliers, purchasing, goods receipts, and warehouse stock through a unified web interface.

![Angular](https://img.shields.io/badge/Angular-20-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-EF_Core-CC2927?style=for-the-badge)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Background_Email-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)

**Clean Architecture · CQRS · CSRF Protection · Multi-company Tenancy · Barcode · Printing · Queued Email**

[Features](#-features) · [Skills demonstrated](#-skills-demonstrated) · [Security and tenancy](#-security-and-tenancy) · [Architecture](#-architecture) · [Email delivery](#-background-email-delivery) · [Run locally](#-run-locally) · [Roadmap](#-roadmap)

</div>

---

## ✨ Features

| Area | Capabilities |
| :--- | :--- |
| **Product catalog** | Products, categories, packaging, and barcode generation |
| **Units of measure** | UOM categories, units, and product-specific conversions |
| **Supplier management** | Supplier records supporting purchasing workflows |
| **Warehouse organization** | Warehouses and storage locations |
| **Purchasing** | Purchase orders with server-side pagination, search, sorting, filters, and printable document views |
| **Goods receiving** | Goods receipt records and printable receipt views |
| **Document printing** | Dedicated purchase-order and goods-receipt preview dialogs with browser printing |
| **Supplier email** | Purchase-order email previews, database outbox, RabbitMQ background processing, and MailKit SMTP delivery |
| **Email delivery tracking** | Pending, Sending, Sent, and Failed states displayed in the purchase-order email dialog |
| **Resilient email processing** | Idempotency keys, persistent queue messages, publisher confirmations, and bounded retries for eligible transient failures |
| **Inventory tracking** | Stock transaction browsing |
| **Dashboard** | Product counts, low-stock indicators, stock valuation, and recent transactions |
| **Company management** | Company records and company-scoped inventory data |
| **Authentication** | ASP.NET Core Identity, JWT access and refresh tokens stored in HttpOnly cookies |
| **Role-aware behavior** | Identity roles and role claims, including Administrator and SuperAdmin |
| **Tenant-aware persistence** | Company context, automatic company assignment, and EF Core global query filters |
| **CSRF protection** | Global server-side antiforgery validation and Angular X-XSRF-TOKEN request headers |
| **Audit metadata** | Creation and modification metadata applied during persistence |
| **Data exchange** | Spreadsheet and CSV export infrastructure |
| **API exploration** | OpenAPI documentation with a Scalar interface in Development |

## 🧠 Skills demonstrated

This project highlights implementation skills across the frontend, backend, and data layers.

| Skill | Evidence in the codebase |
| :--- | :--- |
| **Full-stack development** | Angular feature pages integrated with ASP.NET Core API controllers |
| **Application architecture** | Separate Domain, Application, Infrastructure, API, and Client projects |
| **CQRS and request handling** | Commands and queries organized by feature, using MediatR |
| **Input validation** | FluentValidation validators in the application layer |
| **Relational data modeling** | EF Core entities, SQL Server persistence, and database migrations |
| **Identity and access** | Identity users and roles, JWT validation, and token refresh handling |
| **Browser authentication integration** | HttpOnly authentication cookies, globally validated antiforgery tokens, trusted-origin CORS, and an Angular HTTP interceptor |
| **Multi-tenant data access** | Current-user company context, company assignment on insert, and global query filters |
| **Role-aware application behavior** | Role claims and privileged-role checks in the current-user service |
| **Email integration** | MailKit SMTP transport, purchase-order email previews, and persisted recipient/subject/body snapshots |
| **Background processing** | API-hosted BackgroundService coordinating a database outbox with a durable RabbitMQ queue |
| **Reliability engineering** | Idempotency-key deduplication, transactional outbox claims, delivery status polling, and bounded transient retries |
| **Print workflows** | Dedicated Angular document dialogs and iframe-based browser printing |
| **Audit tracking** | Automatic creation and modification metadata in the EF Core save pipeline |
| **Business workflow modeling** | Purchase orders, goods receipts, stock records, and UOM conversions |
| **Frontend engineering** | TypeScript models and services, Angular routing, and reusable dialogs |
| **UI development** | PrimeNG components, Tailwind CSS, and feature-specific styles |
| **Document and data tooling** | Barcode generation, printable documents, and spreadsheet/CSV libraries |
| **API design** | Feature-based controllers, OpenAPI, and centralized exception handling |
| **Dependency injection** | Service registration and application interfaces separating implementation details |

## 🔐 Security and tenancy

- **Authentication:** Access and refresh tokens use HttpOnly cookies; cookie security settings adapt to HTTPS. Protected inventory controllers require authentication.
- **CSRF protection:** A global `AutoValidateAntiforgeryTokenAttribute` rejects unsafe controller requests with missing or invalid antiforgery tokens with HTTP 400, including login, refresh, and logout. Angular fetches a current-identity token before each mutation and uses the response token in `X-XSRF-TOKEN`. The secure, HttpOnly antiforgery correlation cookie is required alongside it. GET, HEAD, OPTIONS, and TRACE do not require validation.
- **Trusted browser origins:** Credentialed CORS accepts only `Cors:AllowedOrigins`. Development defaults to `http://localhost:4200` and `https://localhost:4200`; other environments require explicit configuration for cross-origin clients. HTTPS is required for the antiforgery cookie.
- **Role-based foundations:** Identity maintains roles and emits role claims. The current-user service checks `Administrator` and `SuperAdmin` for privileged data access. Fine-grained role restrictions on management endpoints remain a roadmap item.
- **Company tenancy:** Inventory entities use company-scoped EF Core query filters, and new company-aware entities inherit the current company when none is supplied. The current implementation lets both `Administrator` and `SuperAdmin` bypass company filters; it also bypasses filters when no authenticated user context exists. These filters are an application data-access mechanism, not a claim of audited tenant isolation.
- **Audit metadata:** The persistence layer records who created or modified auditable entities and when.

The antiforgery flow follows [ASP.NET Core's antiforgery guidance](https://learn.microsoft.com/aspnet/core/security/anti-request-forgery).

## 🏗 Architecture

```mermaid
flowchart LR
    Client[Angular Client] --> API[ASP.NET Core API]
    API --> Application[Application: commands, queries, validation]
    API --> Infrastructure[Infrastructure: persistence and services]
    Application --> Domain[Domain: entities and business models]
    Infrastructure --> Application
    Infrastructure --> Domain
    Infrastructure --> Database[(SQL Server)]
    Infrastructure --> Worker[API-hosted Email Worker]
    Worker <--> Database
    Worker <--> RabbitMQ[Durable RabbitMQ Queue]
    Worker --> SMTP[SMTP Email Server]
```

The Application layer defines use cases and interfaces. Infrastructure implements persistence and external services. The API composes these layers and exposes HTTP endpoints, while the Angular client provides the user interface.

The email worker runs inside the API process. The database retains pending email requests while RabbitMQ is unavailable; the queue coordinates background delivery when the broker is reachable.

```text
src/
├── InventoryManagementSystem.Domain/          # Entities and domain models
├── InventoryManagementSystem.Application/     # Commands, queries, validators, interfaces
├── InventoryManagementSystem.Infrastructure/  # EF Core, Identity, migrations, services
├── InventoryManagementSystem.Api/             # Controllers, middleware, API configuration
└── InventoryManagementSystem.Client/          # Angular pages, models, services, UI
```

## 🛠 Technology stack

| Layer | Technologies |
| :--- | :--- |
| **Frontend** | Angular 20, TypeScript 5.9, RxJS, PrimeNG 20, PrimeIcons, Tailwind CSS 4 |
| **Backend** | C#, ASP.NET Core / .NET 10, MediatR, FluentValidation |
| **Database** | SQL Server, Entity Framework Core 10 |
| **Authentication and security** | ASP.NET Core Identity, JWT bearer authentication, HttpOnly cookies, global antiforgery validation, trusted-origin CORS |
| **Messaging and email** | RabbitMQ.Client 7, BackgroundService, database outbox, MailKit, configurable SMTP transport |
| **Local infrastructure** | Docker Compose for RabbitMQ with a persistent data volume |
| **API tooling** | OpenAPI, Scalar |
| **Data and documents** | ExcelJS, SheetJS, ClosedXML, CsvHelper, JsBarcode |
| **Frontend test tooling** | Jasmine, Karma |

## ✉️ Background email delivery

```mermaid
sequenceDiagram
    participant UI as Angular Client
    participant API as Purchase Order API
    participant DB as SQL Server Outbox
    participant Worker as Email Worker
    participant Queue as RabbitMQ
    participant SMTP as SMTP Server
    UI->>API: Send email with CSRF token and Idempotency-Key
    API->>DB: Save recipient, subject, and body snapshot
    API-->>UI: 202 Accepted with emailId and status URL
    Worker->>DB: Read due Pending requests
    Worker->>Queue: Publish persistent email IDs
    Worker->>Queue: Retrieve queued ID
    Worker->>DB: Claim request and record Sending
    Worker->>SMTP: Send email outside database transaction
    Worker->>DB: Save Sent, retry schedule, or Failed
    Worker->>Queue: Acknowledge handled message
    UI->>API: Poll delivery status
    API-->>UI: Pending / Sending / Sent / Failed
```

| Behavior | Implementation |
| :--- | :--- |
| **Request acceptance** | `POST /api/process/purchase-orders/{id}/send-email` persists an outbox request and returns HTTP 202 |
| **Status lookup** | `GET /api/process/purchase-orders/{id}/emails/{emailId}` returns the company-filtered request status |
| **Deduplication** | Reusing an `Idempotency-Key` UUID for the same purchase order returns the original request and email snapshot |
| **Broker recovery** | Pending database rows remain available when RabbitMQ is down |
| **Retry policy** | Eligible pre-send connection failures and SMTP 4xx rejections receive up to three attempts, with 30- and 60-second retry delays |
| **Uncertain delivery** | Failures during sending and stale Sending requests require delivery review before resubmission |

`Sent` means the SMTP server accepted the message; it does not guarantee inbox delivery. SMTP and SQL Server cannot commit atomically, so the worker does not promise exactly-once delivery. Check uncertain outcomes before sending a new request. The UI retains its idempotency key for request retries within the current component instance; clients needing retries across reloads must persist that key.

## 🚀 Run locally

### Prerequisites

- .NET 10 SDK
- Node.js compatible with the Angular CLI version in the client lockfile
- npm
- A running SQL Server instance with permission to create or migrate the application database
- For background email: Docker Desktop with Linux containers, or another RabbitMQ broker, plus SMTP connection settings

### 1. Clone the repository

```bash
git clone https://github.com/Sitt-Paing/InventoryManagementSystem.git
cd InventoryManagementSystem
```

### 2. Configure the API

Use local .NET user secrets for the connection string and development administrator. The following commands run from the repository root; replace the example values for your environment.

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=InventoryManagement;Trusted_Connection=True;TrustServerCertificate=True" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "JwtSettings:SecretKey" "replace-with-your-own-random-secret-at-least-32-characters" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "DefaultAdmin:UserName" "localadmin" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "DefaultAdmin:Email" "localadmin@example.com" --project src/InventoryManagementSystem.Api
dotnet user-secrets set "DefaultAdmin:Password" "replace-with-your-own-strong-password" --project src/InventoryManagementSystem.Api
```

Choose an administrator password containing uppercase and lowercase letters and a digit, with at least six characters. On a fresh Development database, startup applies migrations for both database contexts and seeds administrator roles and the configured administrator. Existing users are not overwritten by changing seed settings. Check startup logs if database initialization fails.

### 3. Start the backend

```powershell
dotnet dev-certs https --trust
dotnet restore src/InventoryManagementSystem.Api/InventoryManagementSystem.Api.csproj
dotnet run --project src/InventoryManagementSystem.Api --launch-profile https
```

### 4. Start the frontend

In a second terminal:

```bash
cd src/InventoryManagementSystem.Client
npm ci
npm start
```

The development client points to `https://localhost:7152/api` in `src/InventoryManagementSystem.Client/src/environments/environment.ts`.

| Service | Local URL |
| :--- | :--- |
| **Web application** | http://localhost:4200 (use the URL reported by Angular CLI) |
| **API base** | https://localhost:7152/api |
| **API documentation** | https://localhost:7152/docs/scalar |

Sign in with the administrator credentials configured above.

### 5. Optional: enable background purchase-order email

From the repository root, start the included RabbitMQ service:

```powershell
docker compose -f compose.email.yml up -d
```

| RabbitMQ service | Local address |
| :--- | :--- |
| **AMQP** | `localhost:5672` |
| **Management UI** | http://localhost:15672 |

The compose service binds its ports to localhost and persists broker data in a named volume. Local credentials are `guest` / `guest`.

Set `SmtpSettings:Host`, `Port`, `UserName`, `Password`, `FromEmail`, `FromName`, and `UserStartTls` through user secrets or environment variables. `UserStartTls` is the configuration key used by the code. Use your SMTP provider's connection settings and your own test recipient when trying the send action.

Configure `RabbitMqEmail:Enabled` as `true` and restart the API. Development configuration already enables the worker. Broker settings are `Host`, `Port`, `UserName`, `Password`, `VirtualHost`, and `Queue` under `RabbitMqEmail`; the defaults target the local broker and the `inventory.purchase-order-emails` queue.

For example, enable the worker through local user secrets:

```powershell
dotnet user-secrets set "RabbitMqEmail:Enabled" "true" --project src/InventoryManagementSystem.Api
```

Use deployment secrets for broker and SMTP credentials in other environments. With the worker disabled, accepted requests remain Pending until an enabled worker processes them. To try the workflow, preview a purchase order addressed to your own test mailbox, select **Send Email**, and follow its status in the dialog.

### CSRF requirements for API clients

Before any POST, PUT, PATCH, DELETE, or other unsafe controller request, call `GET /api/Auth/csrf-token`, retain the response cookies, and send `data.csrfToken` in `X-XSRF-TOKEN`. Obtain a new token after login, logout, or an authentication identity change. This applies to manual API clients and Scalar requests as well as the Angular app, including bearer-authenticated callers.

For a deployed frontend on another origin, configure `Cors:AllowedOrigins` as an array of exact trusted origins, without trailing slashes. Environment variables can use `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, and so on. Browser restrictions on cross-site cookies still apply.

## 🗺 Roadmap

Upcoming product features:

- [ ] **Reporting:** Add dedicated reporting features.
- [ ] **Expanded management:** Add further management features beyond the existing company and master-data screens.

Security follow-ups identified in the current implementation:

- [x] Enforce server-side antiforgery validation for unsafe controller requests.
- [ ] Apply explicit role authorization to privileged management operations.
- [ ] Refine tenant administrator scope and verify company boundaries across reads and writes.

Reporting and expanded management are planned additions, not completed modules.

## ✅ Development commands

Build the API and its referenced projects from the repository root:

```bash
dotnet build src/InventoryManagementSystem.Api/InventoryManagementSystem.Api.csproj
```

Run the database-free CSRF/CORS integration checks:

```bash
dotnet run --project tests/InventoryManagementSystem.CsrfChecks
```

Run frontend commands from `src/InventoryManagementSystem.Client`:

```bash
npm run build
npm test
```

`npm test` launches the configured Karma/Jasmine runner and requires a compatible browser. These are development commands; their presence does not imply a passing test suite or a published deployment.

---

<div align="center">

**Built by [Sitt-Paing](https://github.com/Sitt-Paing)**

Exploring practical inventory workflows through full-stack software engineering.

</div>
