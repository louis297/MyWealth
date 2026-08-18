# Architecture decision records

Short, dated records of choices we do not want to re-litigate. Copy [`../_templates/adr.md`](../_templates/adr.md) to `NNNN-short-title.md` (4-digit, increment by 1).

Number in the filename, never reuse a number, even if the ADR is superseded.

## When to write one

- Picking or changing the database, identity scheme, host, or frontend
- Changing `BaseEntity` key type, money representation, or tenancy
- Replacing `EnsureCreated` with migrations
- Anything that would surprise someone reading only the function plan

Routine feature work does **not** need an ADR. Use a [feature spec](../features/README.md).

## Index

| ADR | Title | Status |
| --- | --- | --- |
| [0001](0001-use-dotnet-aspire-and-clean-architecture.md) | Use .NET Aspire + Clean Architecture | Accepted |
| [0002](0002-use-mssql-with-aspire.md) | Use MSSQL as Primary Database Managed by Aspire | Accepted |
| [0003](0003-react-redux-typescript-vite-tailwind-frontend.md) | Frontend: React + Redux + TypeScript + Vite + Tailwind CSS | Accepted |
| [0004](0004-money-as-decimal-with-currency.md) | Money Representation as decimal with Currency | Accepted |
| [0005](0005-shared-database-tenantid-isolation.md) | Multi-Tenancy: Shared Database with Row-Level Isolation (TenantId) | Accepted |
| [0006](0006-email-password-jwt-authentication.md) | Authentication: Email/Password + JWT | Accepted |
| [0007](0007-baseentity-primary-key-int.md) | BaseEntity Primary Key Uses int (Identity) | Accepted |
