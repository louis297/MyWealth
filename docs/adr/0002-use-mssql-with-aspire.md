# 0002. Use MSSQL as Primary Database Managed by Aspire

## Status
Accepted

## Date
2026-08-18

## Context
A wealth management platform must store monetary amounts precisely and support transactional data such as accounts, holdings and trade records. The database must:

- Integrate deeply with .NET and EF Core
- Support transactions and complex queries
- Be easy to run locally for development and demos
- Be orchestrated by Aspire

## Decision
We will use Microsoft SQL Server (MSSQL) as the primary database.  
It will be started and managed as a Docker container by .NET Aspire.  
Data access will be performed through EF Core.

## Consequences
Positive:
- Excellent fit with the .NET / EF Core ecosystem
- Native support as an Aspire resource
- Precise storage of monetary values using `decimal`
- One-command local startup via Aspire

Negative:
- Requires Docker
- Switching to PostgreSQL later would be relatively costly
- Image compatibility needs attention on non-Windows environments

## Alternatives considered
1. PostgreSQL — open-source and also supported by Aspire, but the team is more familiar with MSSQL
2. SQLite — unsuitable for multi-tenant and concurrent demo scenarios
3. Azure SQL / Cosmos DB — over-engineered for a simple demo
