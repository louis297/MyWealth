

## title: Glossary status: draft owner: "" last\_updated: 2026-08-16

# Glossary

Shared words. If a feature spec introduces a new term, add it here in the same change.

## Product

| Term | Meaning |
| - | - |
| MyWealth | This application — personal wealth tracking |
| Account | A user-tracked container of value (bank, cash, credit, brokerage, property, …). Not an Identity login. An account belongs to a customer. |
| Holding | A position inside an investment account |
| Transaction | A dated money movement or trade |
| Category | A user (or system) label on a transaction |
| Net worth | Assets minus liabilities at a point in time |
| Money | Amount + ISO currency |
| Client | Also called Tenant or Firm. It is the client of the SaaS platform. |
| User | Authenticated Identity principal. A user can be 4 roles, system admin, tenant admin, adviser, customer. |
| System admin | 1 of the 4 roles. All authorities. Admin of the whole platform. Not bound to any tenant. |
| Tenant admin | 1 of the 4 roles. Full authorities for a client (tenant). Bound to a single tenant. |
| Adviser | 1 of the 4 roles. An adviser can modify customer information or trade. Bound to a single tenant. |
| Customer | 1 of the 4 roles. The end customer. The holder of an account. Bound to a single adviser. |
| Owner | The user whose data a row belongs to |


## Solution

| Term | Meaning |
| - | - |
| AppHost | Aspire host (`src/AppHost`) that starts SQL Server and `webapi` |
| webapi | Aspire resource name for `src/Web` |
| AdviserPortal | Reserved Aspire resource name `adviser-portal`; no project yet |
| MyWealthDb | SQL Server database name and connection-string name |
| Command | MediatR write use case |
| Query | MediatR read use case |
| Endpoint group | `IEndpointGroup` class under `src/Web/Endpoints` |
| Feature spec | One markdown file in `docs/features/` for a vertical slice |
| ADR | Architecture decision record in `docs/adr/` |


## Status of this glossary

Starter product terms are **proposed**. Solution terms match the repo as of 2026-08-16.

