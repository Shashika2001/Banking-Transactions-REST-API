# CLAUDE.md

Guidance for Claude Code when working in this repository: a Banking Transactions REST API built with ASP.NET Core.

## Tech Stack & Compiler Settings

- **Target framework:** .NET 8 (`net8.0`)
- **Language version:** C# 12 (`<LangVersion>12</LangVersion>`)
- **Nullable reference types:** enabled (`<Nullable>enable</Nullable>`)
- **Warnings as errors:** enabled (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- **Implicit usings:** enabled

Put these settings in a shared `Directory.Build.props` at the repository root so every project inherits them. Don't suppress warnings with `#pragma` or `!` (null-forgiving) unless you can justify it in a comment.

## Architecture: Clean Architecture

Keep the layers separate. Dependencies point **inward only**:

```
src/
  BankingApi.Domain/          # Entities, value objects, domain exceptions, domain interfaces. No external dependencies.
  BankingApi.Application/     # Use cases/services, DTOs, validators, interfaces (repositories, clock, etc.). Depends on Domain only.
  BankingApi.Infrastructure/  # EF Core, repositories, external services, configuration. Implements Application interfaces.
  BankingApi.Api/             # ASP.NET Core controllers/endpoints, middleware, DI composition root. Thin; no business logic.
tests/
  BankingApi.UnitTests/       # All unit tests; folders mirror the source projects (e.g. Application/Services/...).
```

Project references: Api → Application, Infrastructure · Infrastructure → Application, Domain · Application → Domain.
`tests/BankingApi.UnitTests/Architecture/LayerDependencyTests.cs` enforces these rules. Keep it passing.

Rules:
- **Domain** has no references to other projects or to frameworks such as EF Core or ASP.NET.
- **Application** defines abstractions (for example `IAccountRepository` or `IDateTimeProvider`). **Infrastructure** implements them.
- **Api** depends on Application, and on Infrastructure only to register services with DI. Controllers call application services and never touch `DbContext` directly.
- Use constructor dependency injection. Don't use service locators or static mutable state.
- Use `decimal` for monetary amounts, never `float` or `double`.

## Testing

- **Every service or business class must have xUnit unit tests.**
- Libraries: **xUnit**, **Moq** for test doubles, and **FluentAssertions** for assertions.
- All unit tests live in `tests/BankingApi.UnitTests`. Folder and namespace mirror the source (`BankingApi.Application.Services.TransferService` → `BankingApi.UnitTests.Application.Services.TransferServiceTests`).
- Use FluentAssertions **7.x**. Version 8 and later need a commercial license.
- Test method naming: `MethodName_StateUnderTest_ExpectedBehavior`.
- Follow Arrange / Act / Assert. Each test checks one behavior.
- Cover the happy path, validation failures, edge cases (zero or negative amounts, insufficient funds, missing accounts), and exception paths.
- Make async tests `async Task`, never `async void`.

## Coding Standards

### Async
- Use `async`/`await` for **all I/O** (database, HTTP, file, messaging).
- Name async methods with the `Async` suffix and accept a `CancellationToken` where appropriate. Pass the token all the way through.
- Never block on async code (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`).

### Secrets & Configuration
- **Never hardcode secrets** such as connection strings, API keys or passwords.
- Use `appsettings.json` for non-secret configuration, and put only placeholders there for anything sensitive.
- Use **user-secrets** (`dotnet user-secrets`) in local development and **environment variables** in other environments.
- Bind configuration with the strongly typed Options pattern (`IOptions<T>`) and validate it at startup (`ValidateDataAnnotations()` / `ValidateOnStart()`).

### Input Validation
- **Validate all external input**: request bodies, route and query parameters, headers, and configuration.
- Validate at the API boundary (for example with FluentValidation or data annotations) and enforce invariants in the Domain layer as well.
- Return consistent error responses using RFC 7807 `ProblemDetails`. Don't leak stack traces or internal details.

### Naming, Structure & Documentation
- Use meaningful, intention-revealing names. Avoid abbreviations and single-letter names, except for loop indices and lambdas.
- Keep methods small and focused on one responsibility. Extract helpers when a method grows long or deeply nested.
- Add **XML doc comments (`///`)** to all public types and members. Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` so missing docs are caught; with warnings as errors, they fail the build.
- Follow standard .NET naming conventions: PascalCase for types and members, camelCase for locals and parameters, `_camelCase` for private fields, and an `I` prefix for interfaces.

## Workflow: Definition of Done

After **every** task:

1. Run `dotnet build` and fix every error and warning (warnings are errors).
2. Run `dotnet test` and fix every failing test.
3. Don't call the task finished until both commands pass. If something can't be fixed, say so plainly and include the output.
4. End with a suggested **Conventional Commit** message using one of these prefixes:
   - `feat:` for a new feature
   - `fix:` for a bug fix
   - `test:` for adding or updating tests
   - `docs:` for documentation only
   - `chore:` for tooling, config, dependencies or housekeeping

   Example: `feat: add endpoint to transfer funds between accounts`

## Common Commands

```bash
dotnet build                      # Build the solution
dotnet test                       # Run all tests
dotnet run --project src/BankingApi.Api
dotnet user-secrets set "ConnectionStrings:BankingDb" "<value>" --project src/BankingApi.Api
```
