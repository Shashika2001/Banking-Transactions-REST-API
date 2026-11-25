# Banking Transactions REST API

A REST API for banking transactions, built with ASP.NET Core (.NET 8) using Clean Architecture.

## Authentication

All endpoints except `POST /api/auth/register` and `POST /api/auth/login` require a JWT bearer token
(`Authorization: Bearer <accessToken>`). In Swagger UI, click **Authorize** and paste the `accessToken`.

| Role | Access |
| --- | --- |
| `Customer` | Created by `register`. Only their own customer record, accounts, transactions and transfers *from* their own accounts. |
| `Admin` | Everything, including customer listing/creation and audit logs. Created only via `BootstrapAdmin` configuration. |

`POST /api/auth/login` is rate limited per client IP (default 5 attempts per 60 seconds; `RateLimiting:Login`).
National IDs are always masked in responses (only the last 4 characters are shown).

## Endpoints

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/api/auth/register` | Register as a customer (anonymous) |
| `POST` | `/api/auth/login` | Sign in and get a token (anonymous, rate limited) |
| `POST` | `/api/customers` | Create a customer (Admin) |
| `GET` | `/api/customers/{customerId}` | Get a customer |
| `GET` | `/api/customers?pageNumber=1&pageSize=20` | List customers (Admin) |
| `POST` | `/api/accounts` | Open an account |
| `GET` | `/api/accounts/{accountId}` | Get an account |
| `GET` | `/api/accounts/{accountId}/balance` | Get an account balance |
| `POST` | `/api/accounts/{accountId}/deposits` | Deposit money |
| `POST` | `/api/accounts/{accountId}/withdrawals` | Withdraw money |
| `POST` | `/api/transfers` | Transfer between accounts (requires `Idempotency-Key` header) |
| `GET` | `/api/accounts/{accountId}/transactions?from=&to=&pageNumber=&pageSize=` | Transaction history (`from` inclusive, `to` exclusive) |
| `GET` | `/api/audit-logs?userId=&action=&entityName=&entityId=&from=&to=` | Search audit logs (Admin) |

Errors are returned as RFC 7807 problem details: 400 validation, 401 unauthenticated or failed login,
403 forbidden, 404 not found, 409 conflict
(concurrent update, duplicate data, or an idempotency key reused for a different request), 422 business rule
violation (with an `errorCode` such as `InsufficientFunds` or `DailyTransferLimitExceeded`), 429 rate limited,
500 unexpected error.

Swagger UI is available at `/swagger` in Development.

## Configuration

| Key | Default | Description |
| --- | --- | --- |
| `ConnectionStrings:BankingDb` | — | SQL Server connection string (use user-secrets / environment variables) |
| `Jwt:SigningKey` | — | **Secret.** HMAC key, at least 32 bytes (user-secrets / `Jwt__SigningKey`) |
| `Jwt:Issuer` / `Jwt:Audience` | `BankingApi` / `BankingApi.Clients` | Token issuer and audience |
| `Jwt:AccessTokenLifetimeMinutes` | `60` | Token lifetime (1–1440) |
| `BootstrapAdmin:Email` / `BootstrapAdmin:Password` | — | **Secret.** Optional first admin, created at startup if missing |
| `Transfers:DailyLimitPerAccount` | `10000` | Maximum total each account may transfer out per UTC day |
| `RateLimiting:Login:PermitLimit` / `WindowSeconds` | `5` / `60` | Login attempts allowed per client IP per window |

## Solution structure

| Project | Responsibility |
| --- | --- |
| `src/BankingApi.Domain` | Entities, enums, domain exceptions. No dependencies. |
| `src/BankingApi.Application` | Services, DTOs, interfaces, validation. Depends on Domain. |
| `src/BankingApi.Infrastructure` | EF Core, SQL Server, repositories. Depends on Application and Domain. |
| `src/BankingApi.Api` | ASP.NET Core Web API controllers and composition root. Depends on Application and Infrastructure. |
| `tests/BankingApi.UnitTests` | xUnit unit tests (Moq, FluentAssertions). |

## Prerequisites

- .NET 8 SDK
- SQL Server (local instance or container)

## Getting started

```bash
# Configure secrets (never commit them)
dotnet user-secrets set "ConnectionStrings:BankingDb" "<your-connection-string>" --project src/BankingApi.Api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/BankingApi.Api

# Optional: create the first admin on startup
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.com" --project src/BankingApi.Api
dotnet user-secrets set "BootstrapAdmin:Password" "<at least 12 characters>" --project src/BankingApi.Api

dotnet build
dotnet test
dotnet run --project src/BankingApi.Api
```

In non-development environments, set the connection string with the `ConnectionStrings__BankingDb` environment variable.
