# Haggly MVP Architecture

## Direction

Haggly will begin as a **modular monolith** on .NET 10. The goal is to keep the MVP simple while isolating business logic from frameworks and external services, allowing technology changes and future service extraction without designing a distributed system prematurely.

## Technology stack

- .NET 10 and C# 14
- ASP.NET Core 10 Minimal APIs
- Entity Framework Core 10 for commands and transactional writes
- Dapper only where custom read queries provide a clear benefit
- One relational database for the MVP
- FluentValidation
- ASP.NET Core authentication and policy-based authorization
- Problem Details for consistent API errors
- OpenAPI
- xUnit, FluentAssertions, and Testcontainers
- Structured logging and OpenTelemetry when operationally needed

## Solution structure

```text
Haggly/
├── Haggly.slnx
├── src/
│   ├── Haggly.Domain/
│   │   ├── Common/
│   │   └── Modules/
│   │       ├── Identity/
│   │       ├── Markets/
│   │       ├── Catalog/
│   │       ├── Inventory/
│   │       ├── Sales/
│   │       │   ├── Orders/
│   │       │   ├── Negotiations/
│   │       │   └── Fulfillments/
│   │       ├── Payments/
│   │       └── Finance/
│   ├── Haggly.Application/
│   │   ├── Abstractions/
│   │   ├── Behaviors/
│   │   └── Modules/
│   │       ├── Identity/
│   │       ├── Markets/
│   │       ├── Catalog/
│   │       ├── Inventory/
│   │       ├── Sales/
│   │       ├── Payments/
│   │       └── Finance/
│   ├── Haggly.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Authentication/
│   │   ├── Payments/
│   │   ├── Messaging/
│   │   └── Time/
│   └── Haggly.Api/
│       ├── Endpoints/
│       ├── Middleware/
│       ├── Authorization/
│       └── OpenApi/
├── tests/
│   ├── Haggly.UnitTests/
│   ├── Haggly.IntegrationTests/
│   └── Haggly.ArchitectureTests/
├── docs/
│   └── architecture.md
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
└── README.md
```

## Project responsibilities

### Domain

Contains aggregates, entities, value objects, domain events, business rules, and domain errors. It must not depend on ASP.NET Core, EF Core, Dapper, or external providers.

### Application

Contains commands, queries, handlers, validators, authorization requirements, and interfaces for external dependencies. Organize code by business use case rather than broad technical folders.

Example:

```text
Modules/Inventory/
├── OpenInventorySession/
├── AdjustStock/
├── ReserveStock/
├── ReleaseReservation/
└── GetDailyListings/
```

### Infrastructure
