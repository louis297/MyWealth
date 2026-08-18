# 0001. Use .NET Aspire + Clean Architecture

## Status
Accepted

## Date
2026-08-18

## Context
MyWealth is a simple wealth management SaaS demo project intended for learning platform architecture. The solution needs to:

- Orchestrate infrastructure (database, caches, messaging, etc.) quickly and reliably
- Keep the backend structure clear, maintainable and extensible
- Decouple frontend from backend
- Support rapid iteration by a small team or individual while still demonstrating enterprise-grade practices

## Decision
We will use .NET Aspire as the application host and infrastructure orchestration tool.  
The backend will follow Clean Architecture (layers: Domain / Application / Infrastructure / Presentation).  
The frontend will be an independent React + Redux + TypeScript application.

## Consequences
Positive:
- Aspire provides unified management of SQL Server containers, connection strings, health checks and service discovery
- Clean Architecture keeps business logic independent of infrastructure, improving testability and replaceability
- The overall structure serves as a solid reference architecture for learning and demonstration

Negative:
- Higher initial learning curve (Aspire + Clean Architecture)
- Project structure is more complex than a simple CRUD application
- Requires discipline to maintain layer boundaries

## Alternatives considered
1. Minimal API + simple three-layer structure — too simplistic, loses educational value
2. Manual Docker Compose orchestration — lacks Aspire’s developer experience and type-safe configuration
3. Other orchestration tools (e.g. Tye) — superseded by Aspire
