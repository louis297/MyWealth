# 0005. Multi-Tenancy: Shared Database with Row-Level Isolation (TenantId)

## Status
Accepted

## Date
2026-08-18

## Context
MyWealth is positioned as a SaaS wealth-management demo and therefore must demonstrate multi-tenancy. A balance is required between implementation simplicity and adequate isolation, while remaining friendly to EF Core and Aspire.

## Decision
We will use a **shared database with row-level isolation**:
- Every business table contains a `TenantId` column
- Data isolation is enforced by EF Core Global Query Filters (and/or application-layer checks)
- The current tenant is resolved from a claim inside the JWT (or from a request header)

## Consequences
Positive:
- Simple to implement and ideal for a learning / demo scenario
- Low database management overhead (single database)
- EF Core Global Query Filters provide solid support
- Aspire configuration and migrations stay straightforward

Negative:
- Isolation is weaker than separate schemas or separate databases
- Every query must correctly apply the TenantId filter; otherwise data leakage is possible
- Large differences in tenant data volume may make performance tuning harder later

## Alternatives considered
1. Separate schema per tenant — stronger isolation but more complex Aspire + EF Core migration and connection management
2. Separate database per tenant — strongest isolation but operationally heavy for a demo
3. No multi-tenancy design — contradicts the SaaS platform positioning
