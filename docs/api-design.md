---
title: API design
status: draft
owner: ""
last_updated: 2026-08-20
related:
  - architecture.md
  - function-plan.md
  - domain-model.md
  - database-design.md
  - glossary.md
  - adr/0001-use-dotnet-aspire-and-clean-architecture.md
  - adr/0004-money-as-decimal-with-currency.md
  - adr/0005-shared-database-tenantid-isolation.md
  - adr/0006-email-password-jwt-authentication.md
  - adr/0007-baseentity-primary-key-int.md
---

# API design

HTTP surface of `webapi`. Endpoint groups live in `src/Web/Endpoints`. Each group implements `IEndpointGroup` and is discovered by `MapEndpoints`.

This document describes the MVP API for the Adviser Portal. It is derived from the function plan, domain model, database design, and accepted ADRs.

## 1. Conventions

| Topic | Convention |
| --- | --- |
| Style | Minimal APIs, typed results (`Ok`, `Created`, `NoContent`, `BadRequest`, …) |
| Dispatch | Endpoint sends a MediatR command/query. No `DbContext` in endpoints. |
| Auth default | `groupBuilder.RequireAuthorization()` on the group, unless it is the Identity group |
| Route | Prefer kebab-case (`/accounts`, `/net-worth`). Group name ≈ type name. |
| Ids | Route `{id}` must match the command's `Id` on updates; otherwise `400` |
| Create | `201 Created` with the new `int` id |
| Update / disable | `204 NoContent` |
| Get one / list | `200 Ok` + view model |
| Validation | FluentValidation → `ValidationException` → problem details |
| Forbidden | `ForbiddenAccessException` → `403` |
| Missing / invisible resource | Always `404` (do not leak existence across tenants or ownership) |
| Docs | `[EndpointSummary]` + `[EndpointDescription]` on every action |
| Explore | Scalar at `/scalar` (root `/` may redirect there) |
| Versioning | None in MVP |
| CORS | Wide open in Development. Tighten only after MVP. |

## 2. Authentication and authorization

### 2.1 Authentication

- Scheme: email + password + JWT (ASP.NET Identity Bearer tokens via `MapIdentityApi<ApplicationUser>`).
- Additional endpoint: `POST /users/logout` (requires authentication).
- Current user: `IUser` / `Web/Services/CurrentUser.cs`.
- Customer role login is deliberately disabled in the Application / Identity layer for MVP.

### 2.2 Roles and visibility

| Role | Login in MVP? | Tenant binding | Data scope |
| --- | --- | --- | --- |
| SystemAdmin | Yes | None (`TenantId = null`) | Only `/tenants`. Business data is invisible. |
| TenantAdmin | Yes | Exactly one Tenant | All Advisers, Customers, Accounts, Holdings and Transactions inside that tenant |
| Adviser | Yes | Exactly one Tenant | Only Customers assigned to them, plus those Customers' Accounts, Holdings and Transactions |
| Customer | **No** | Exactly one Tenant + required `AdviserId` | None. Cannot authenticate. |

Notes:

- All four roles live in the same `Users` table.
- Customer must have both a non-null `TenantId` and a non-null `AdviserId` pointing to an Adviser in the same tenant.
- SystemAdmin follows **Option A**: it can manage tenants only. Business endpoints rely on the caller's `TenantId`; because SystemAdmin has `TenantId = null`, those endpoints naturally return no data. No `TenantId` is required in route templates.
- Future expansion (Header-based tenant context switching for SystemAdmin) is possible but out of MVP.

### 2.3 Disable / close strategy (no hard deletes of core data)

| Resource | Behaviour on “delete” | Recoverable? |
| --- | --- | --- |
| Account | Set `Status = Closed` | **No** — permanent |
| Adviser / Customer | Set `IsEnabled = false` | Yes (re-enable is allowed) |
| Adviser disable | Must reassign all Customers first | — |

- Closed accounts reject all new Holdings and Transactions and are excluded from Net Worth calculations.
- Related Holdings and Transactions are retained for history; they are not physically deleted.
- A future maintenance script may archive Closed / Disabled rows into separate tables. That capability is not exposed via the API.

### 2.4 Authorization rules for every new resource endpoint

1. Require authorization on the group.
2. Scope every query and command to the caller's visibility (Tenant + Adviser ownership where applicable).
3. Return `404` (never `403`) when the id exists but is outside the caller's scope.
4. Apply role checks via `[Authorize(Roles = …)]` or custom policies enforced by `AuthorizationBehaviour`.

## 3. Error shape

| Situation | HTTP | Body |
| --- | --- | --- |
| Validation failure | 400 | Validation problem details (FluentValidation errors) |
| Unauthenticated | 401 | — |
| Authenticated but not allowed | 403 | — |
| Missing / other-tenant / other-adviser resource | 404 | — |
| Business rule violation (e.g. post to Closed account) | 400 | Problem details |
| Unhandled | 500 | Exception handler |

## 4. Resource catalog (MVP)

| Resource | Base route | Methods | Auth | Notes |
| --- | --- | --- | --- | --- |
| Identity / Users | Identity paths + `/users/logout` | Login, Logout | Mixed | Customer login disabled |
| Tenants | `/tenants` | GET list, GET id, POST, PUT | SystemAdmin only | |
| Advisers | `/advisers` | GET list, GET id, POST, PUT, disable | TenantAdmin | |
| Customers | `/customers` | GET list, GET id, POST, PUT, disable | TenantAdmin, Adviser | Must supply `AdviserId` on create |
| Accounts | `/accounts` | GET list, GET id, POST, PUT, close | TenantAdmin, Adviser | Currency immutable after create; close is permanent |
| Holdings | `/accounts/{accountId}/holdings` | GET, POST, PUT, DELETE | TenantAdmin, Adviser | Nested under Account |
| Transactions | `/transactions` | GET (filter), POST | TenantAdmin, Adviser | Append-only; no update/delete |
| Dashboard | `/dashboard/net-worth`, `/dashboard/allocation` | GET | TenantAdmin, Adviser | Supports optional `customerId` |
| Audit Logs | `/audit-logs` | GET | TenantAdmin (own tenant), SystemAdmin (all) | |

## 5. Endpoint details

### 5.1 Identity

- Login / refresh / etc. come from `MapIdentityApi<ApplicationUser>`.
- `POST /users/logout` — requires authentication.
- No `/me` endpoint in MVP. The client stores claims from the JWT.

### 5.2 Tenants (SystemAdmin only)

| Method | Route | Description |
| --- | --- | --- |
| GET | `/tenants` | Paginated list |
| GET | `/tenants/{id}` | Detail |
| POST | `/tenants` | Create (Name required and unique) |
| PUT | `/tenants/{id}` | Rename or enable/disable |

### 5.3 Advisers (TenantAdmin)

| Method | Route | Description |
| --- | --- | --- |
| GET | `/advisers` | List (search + pagination) inside current tenant |
| GET | `/advisers/{id}` | Detail |
| POST | `/advisers` | Create Adviser + corresponding Identity user |
| PUT | `/advisers/{id}` | Update details or enable/disable |
| DELETE (soft) | `/advisers/{id}` | Disable. Must reassign all Customers first. |

### 5.4 Customers (TenantAdmin + Adviser)

| Method | Route | Description |
| --- | --- | --- |
| GET | `/customers` | List. Advisers see only their own Customers. Search + pagination. |
| GET | `/customers/{id}` | Detail + account overview |
| POST | `/customers` | Create. **`AdviserId` is required.** No login is enabled. |
| PUT | `/customers/{id}` | Update details or reassign Adviser |
| DELETE (soft) | `/customers/{id}` | Disable (`IsEnabled = false`). No physical delete. |

### 5.5 Accounts

| Method | Route | Description |
| --- | --- | --- |
| GET | `/accounts` | List filtered by `customerId` |
| GET | `/accounts/{id}` | Detail (total value, holdings overview, recent transactions) |
| POST | `/accounts` | Create. Requires `CustomerId`, `Type`, `Currency`. |
| PUT | `/accounts/{id}` | Update Name / Type. Currency is immutable. |
| Close | `/accounts/{id}/close` or Status update | Set `Status = Closed`. **Irreversible.** |

Constraints:

- Closed accounts reject new Holdings and Transactions.
- Closed accounts are excluded from Net Worth.
- Related Holdings and Transactions are kept for history.

### 5.6 Holdings (nested)

| Method | Route | Description |
| --- | --- | --- |
| GET | `/accounts/{accountId}/holdings` | All holdings of the account |
| POST | `/accounts/{accountId}/holdings` | Create holding |
| PUT | `/accounts/{accountId}/holdings/{id}` | Update |
| DELETE | `/accounts/{accountId}/holdings/{id}` | Remove (or soft-delete if preferred later) |

Key fields:

- `Instrument` (Name required, Symbol optional)
- `Quantity` ≥ 0
- `CostBasis` (Money) — currency must match the Account

### 5.7 Transactions

| Method | Route | Description |
| --- | --- | --- |
| GET | `/transactions` | Filter by AccountId, date range, Type + pagination |
| POST | `/transactions` | Create a transaction |

Rules:

- Append-only: no PUT, no DELETE.
- On success return only the new id (`201 Created`). The client re-queries the Holding if it needs the updated position.
- Buy / Sell require `HoldingId` + `Quantity` and automatically adjust the Holding’s quantity and average cost basis inside the Account aggregate.
- TransferIn / TransferOut / Dividend / Interest are cash-only and do not touch Holdings.
- Cannot post to a Closed account.
- `Amount.Currency` must equal the Account’s currency.

### 5.8 Dashboard

| Method | Route | Description |
| --- | --- | --- |
| GET | `/dashboard/net-worth` | Net worth for the caller’s visible scope |
| GET | `/dashboard/net-worth?customerId={id}` | Net worth for a specific Customer (must be visible to the caller) |
| GET | `/dashboard/allocation` | Asset allocation by AccountType |

Calculation rules follow the domain model (Closed accounts excluded, Credit treated as liability, Brokerage/Property use CostBasis in MVP).

### 5.9 Audit Logs

| Method | Route | Description |
| --- | --- | --- |
| GET | `/audit-logs` | Paginated list. Filter by time, actor, subject type. |

Visibility: TenantAdmin → own tenant only; SystemAdmin → all.

## 6. Request / response conventions

- List endpoints support `page`, `pageSize`, optional `search` and sort.
- Money is always returned as:

```json
{
  "amount": 12345.67,
  "currency": "NZD"
}
```

- Enums should expose both `value` and a human-readable `displayName` when useful for the UI.
- Successful create responses contain `{ "id": 123 }`.
- Dates use ISO 8601 / `DateOnly` as appropriate.

## 7. Endpoint checklist

When adding a group:

- [ ] New file `src/Web/Endpoints/<Name>.cs` implementing `IEndpointGroup`
- [ ] `[EndpointSummary]` / `[EndpointDescription]`
- [ ] Command/query + validator in Application
- [ ] Role and data-scope checks (Tenant + Adviser ownership)
- [ ] Feature spec in `docs/features/` (when the feature is implemented)
- [ ] Functional tests in `tests/Application.FunctionalTests`
- [ ] Row added to the catalog above

## 8. Explicitly out of scope for MVP

- Customer login / Customer Portal
- Update or delete of Transactions
- Physical deletion of Accounts, Customers or Advisers
- Re-opening a Closed Account
- Historical Net Worth snapshots
- Custom transaction categories
- Automatic multi-currency conversion
- Report export (CSV / PDF)
- `/me` profile endpoint
- API versioning
- Production CORS tightening
- Data-archival / clean-up APIs (future maintenance scripts only)

## 9. Confirmed decisions

| Topic | Decision |
| --- | --- |
| SystemAdmin scope | Option A — manage Tenants only; business data invisible |
| Holdings routes | Nested under `/accounts/{accountId}/holdings` |
| Account close | `Status = Closed`, permanent, no re-activation |
| Customer / Adviser removal | Soft disable (`IsEnabled = false`) + mandatory reassignment for Advisers |
| Net Worth | Supports optional `customerId` filter |
| `/me` | Not implemented in MVP |
| Transaction create response | Return only success + id; client re-fetches Holding if needed |
| Future clean-up | Possible offline script that moves Closed/Disabled rows to archive tables |

## 10. Changelog

| Date | Change |
| --- | --- |
| 2026-08-20 | Full MVP design written from function-plan, domain-model, database-design and confirmed discussion points (soft-disable, nested holdings, SystemAdmin Option A, irreversible Account close, etc.) |
| 2026-08-16 | Template created; starter endpoint groups listed |
