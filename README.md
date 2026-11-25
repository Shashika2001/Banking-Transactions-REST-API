# Banking Transactions REST API

A REST API for banking transactions, built with ASP.NET Core (.NET 8) using Clean Architecture.

> 🚧 Work in progress. Features and endpoints will be documented here as they are built.

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
