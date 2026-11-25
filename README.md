# Banking Transactions REST API

A REST API for banking transactions, built with ASP.NET Core (.NET 8) using Clean Architecture.

## Endpoints

| Method | Route | Description |
| --- | --- | --- |
| `POST` | `/api/customers` | Create a customer |
| `GET` | `/api/customers/{customerId}` | Get a customer |
| `GET` | `/api/customers?pageNumber=1&pageSize=20` | List customers |
| `POST` | `/api/accounts` | Open an account |
| `GET` | `/api/accounts/{accountId}` | Get an account |
| `GET` | `/api/accounts/{accountId}/balance` | Get an account balance |
| `POST` | `/api/accounts/{accountId}/deposits` | Deposit money |
| `POST` | `/api/accounts/{accountId}/withdrawals` | Withdraw money |
| `POST` | `/api/transfers` | Transfer between accounts (requires `Idempotency-Key` header) |
| `GET` | `/api/accounts/{accountId}/transactions?from=&to=&pageNumber=&pageSize=` | Transaction history (`from` inclusive, `to` exclusive) |

Errors are returned as RFC 7807 problem details: 400 validation, 404 not found, 409 conflict
(concurrent update, duplicate data, or an idempotency key reused for a different request), 422 business rule
violation (with an `errorCode` such as `InsufficientFunds` or `DailyTransferLimitExceeded`), 500 unexpected error.

Swagger UI is available at `/swagger` in Development.

## Configuration

| Key | Default | Description |
| --- | --- | --- |
| `ConnectionStrings:BankingDb` | — | SQL Server connection string (use user-secrets / environment variables) |
| `Transfers:DailyLimitPerAccount` | `10000` | Maximum total each account may transfer out per UTC day |

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
# Configure the database connection (never commit secrets)
dotnet user-secrets set "ConnectionStrings:BankingDb" "<your-connection-string>" --project src/BankingApi.Api

dotnet build
dotnet test
dotnet run --project src/BankingApi.Api
```

In non-development environments, set the connection string with the `ConnectionStrings__BankingDb` environment variable.
