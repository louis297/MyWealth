---
title: API design
status: draft
owner: ""
last_updated: 2026-08-24
related:
  - architecture.md
  - function-plan.md
  - domain-model.md
  - database-design.md
  - glossary.md
  - features/identity-auth.md
  - features/tenants.md
  - features/tenant-admins.md
  - features/advisers.md
  - features/customers.md
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
| Auth default | `groupBuilder.RequireAuthorization()` on the group, unless it is the auth/login group |
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

- Scheme: email + password + JWT (ASP.NET Identity + Bearer tokens).
- Custom routes are used instead of exposing the raw `MapIdentityApi` surface.
- Login: `POST /auth/login` (anonymous). Returns a JWT on success.
- Logout: `POST /auth/logout` (requires authentication).
- Current user profile: `GET /users/me`, `PUT /users/me`, `PUT /users/me/password`.
- Current user abstraction: `IUser` / `Web/Services/CurrentUser.cs`.
- **Customer role login is deliberately rejected with 403** in the Application layer for MVP.
- JWT must contain at least: user id, email, role, tenantId (null for SystemAdmin).

### 2.2 Roles and visibility

| Role | Login in MVP? | Tenant binding | Data scope |
| --- | --- | --- | --- |
| SystemAdmin | Yes | None (`TenantId = null`) | Only `/tenants` and `/tenant-admins`. Business data is invisible. |
| TenantAdmin | Yes | Exactly one Tenant | All Advisers, Customers, Accounts, Holdings and Transactions inside that tenant |
| Adviser | Yes | Exactly one Tenant | Only Customers assigned to them, plus those Customers' Accounts, Holdings and Transactions |
| Customer | **No** | Exactly one Tenant + required `AdviserId` | None. Cannot authenticate. |

Notes:

- All four roles live in the same `Users` table.
- Customer must have both a non-null `TenantId` and a non-null `AdviserId` pointing to an Adviser in the same tenant.
- SystemAdmin follows **Option A**: it can manage Tenants and TenantAdmins only. Business endpoints rely on the caller's `TenantId`; because SystemAdmin has `TenantId = null`, those endpoints naturally return no data. No `TenantId` is required in route templates.
- Future expansion (Header-based tenant context switching for SystemAdmin) is possible but out of MVP.

### 2.3 Disable / close strategy (no hard deletes of core data)

| Resource | Behaviour on “delete” | Recoverable? |
| --- | --- | --- |
| Account | Set `Status = Closed` | **No** — permanent |
| Adviser / Customer / TenantAdmin | Set `IsEnabled = false` | Yes (re-enable is allowed) |
| Adviser disable | Must reassign all Customers first | — |
| TenantAdmin disable | Last remaining TenantAdmin of a Tenant is allowed | — |

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
| Customer role attempts to log in | **403** | — |
| Missing / other-tenant / other-adviser resource | 404 | — |
| Business rule violation (e.g. post to Closed account) | 400 | Problem details |
| Unhandled | 500 | Exception handler |

## 4. Resource catalog (MVP)

| Resource | Base route | Methods | Auth | Notes |
| --- | --- | --- | --- | --- |
| Auth | `/auth` | login, logout | Mixed | Custom routes. Customer login → 403 |
| Current user | `/users/me` | GET, PUT, PUT password | user | Profile (non-password) + password change |
| Tenants | `/tenants` | GET list, GET id, POST, PUT | SystemAdmin only | |
| Tenant Admins | `/tenant-admins` | GET list, GET id, POST, PUT, disable | SystemAdmin only | Create also creates Identity user. Target Tenant must be enabled. |
| Advisers | `/advisers` | GET list, GET id, POST, PUT, disable | TenantAdmin | |
| Customers | `/customers` | GET list, GET id, POST, PUT, disable | TenantAdmin, Adviser | Must supply `AdviserId` on create |
| Accounts | `/accounts` | GET list, GET id, POST, PUT, close | TenantAdmin, Adviser | Currency immutable after create; close is permanent |
| Holdings | `/accounts/{accountId}/holdings` | GET, POST, PUT, DELETE | TenantAdmin, Adviser | Nested under Account |
| Transactions | `/transactions` | GET (filter), POST | TenantAdmin, Adviser | Append-only; no update/delete |
| Dashboard | `/dashboard/net-worth`, `/dashboard/allocation` | GET | TenantAdmin, Adviser | Supports optional `customerId` |
| Audit Logs | `/audit-logs` | GET | TenantAdmin (own tenant), SystemAdmin (all) | |

## 5. Endpoint details

### 5.1 Auth & Current user

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| POST | `/auth/login` | anonymous | 200 + token payload | 400, 401, **403 (Customer role)** | Email + password → JWT |
| POST | `/auth/logout` | user | 204 | 401 | Sign out |
| GET | `/users/me` | user | 200 + CurrentUserVm | 401 | Current user profile |
| PUT | `/users/me` | user | 204 | 400, 401 | Update non-password profile fields |
| PUT | `/users/me/password` | user | 204 | 400, 401 | Change password (requires current password) |

Rules:

- Customer-role accounts always receive **403** on login, even if the credentials are otherwise valid.
- `PUT /users/me` may change only non-password profile fields (e.g. display name). Email, Role, TenantId and IsEnabled are immutable through this endpoint.
- `PUT /users/me/password` requires the current password and a new password that satisfies Identity password rules.
- JWT claims must include at least: user id, email, role, tenantId (null for SystemAdmin).

See feature spec: [identity-auth](features/identity-auth.md).

### 5.2 Tenants (SystemAdmin only)

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/tenants` | SystemAdmin | 200 + paginated list | 400, 401, 403 | List with pagination, `isEnabled` filter, `search` (Id or Name) |
| GET | `/tenants/{id}` | SystemAdmin | 200 + TenantVm | 401, 403, 404 | Single Tenant |
| POST | `/tenants` | SystemAdmin | 201 + `{ "id": n }` | 400, 401, 403 | Create. Does not create a user. |
| PUT | `/tenants/{id}` | SystemAdmin | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled. Route `{id}` must match body `id`. |

Query parameters for list (reuse this shape for Advisers / Customers / Accounts):

- `page` (1-based, default 1, min 1)
- `pageSize` (default 20, min 1, max 100)
- `isEnabled` (optional bool)
- `search` (optional string — matches Id exactly or Name contains, case-insensitive)

`TenantVm`: `{ "id": 1, "name": "Acme", "isEnabled": true }`

Paginated list:

```json
{
  "items": [{ "id": 1, "name": "Acme", "isEnabled": true }],
  "pageNumber": 1,
  "totalPages": 1,
  "totalCount": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

PUT is partial: omitted `name` / `isEnabled` stay unchanged. Supplying neither is 400. Disabled tenants remain visible to SystemAdmin.

See feature spec: [tenants](features/tenants.md).

### 5.3 Tenant Admins (SystemAdmin)

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- |
| GET | `/tenant-admins` | SystemAdmin | 200 + paginated list | 400, 401, 403 | List with pagination, `isEnabled` / `tenantId` filters, `search` (Id, Name or Email) |
| GET | `/tenant-admins/{id}` | SystemAdmin | 200 + TenantAdminVm | 401, 403, 404 | Single TenantAdmin (non-TenantAdmin ids are 404) |
| POST | `/tenant-admins` | SystemAdmin | 201 + `{ "id": n }` | 400, 401, 403 | Create Domain User + Identity login. Email globally unique. Target Tenant must exist and be enabled. |
| PUT | `/tenant-admins/{id}` | SystemAdmin | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled. Route `{id}` must match body `id`. |
| DELETE | `/tenant-admins/{id}` | SystemAdmin | 204 | 400, 401, 403, 404 | Soft-disable (`IsEnabled = false` on Domain User and Identity). Last admin is allowed. |

Query parameters for list (same shape as Tenants / Advisers, plus optional Tenant filter):

- `page` (1-based, default 1, min 1)
- `pageSize` (default 20, min 1, max 100)
- `isEnabled` (optional bool)
- `tenantId` (optional int — exact match)
- `search` (optional string — matches Id exactly or Name/Email contains, case-insensitive)

POST body:

```json
{
  "tenantId": 1,
  "name": "Alice Chen",
  "email": "alice.chen@acme.com",
  "password": "P@ssw0rd!"
}
```

`TenantAdminVm`: `{ "id": 5, "tenantId": 1, "tenantName": "Acme Wealth", "name": "Alice Chen", "email": "alice.chen@acme.com", "isEnabled": true }`

PUT is partial: omitted `name` / `isEnabled` stay unchanged. Supplying neither is 400. Re-enable is `PUT` with `isEnabled: true` and has no extra preconditions.

Missing or disabled Tenant on create returns **400** (never 404). Non-TenantAdmin ids return **404**.

See feature spec: [tenant-admins](features/tenant-admins.md).

### 5.4 Advisers (TenantAdmin)

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- |
| GET | `/advisers` | TenantAdmin | 200 + paginated list | 400, 401, 403 | List with pagination, `isEnabled` filter, `search` (Id, Name or Email) |
| GET | `/advisers/{id}` | TenantAdmin | 200 + AdviserVm | 401, 403, 404 | Single Adviser (tenant-scoped; non-Adviser ids are 404) |
| POST | `/advisers` | TenantAdmin | 201 + `{ "id": n }` | 400, 401, 403 | Create Domain User + Identity login. Email globally unique. |
| PUT | `/advisers/{id}` | TenantAdmin | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled. Route `{id}` must match body `id`. Disabling fails if Customers are still assigned. |
| DELETE | `/advisers/{id}` | TenantAdmin | 204 | 400, 401, 403, 404 | Soft-disable (`IsEnabled = false` on Domain User and Identity). Fails with 400 if any Customer still has this AdviserId. |

Query parameters for list (same shape as Tenants, plus Email in search):

- `page` (1-based, default 1, min 1)
- `pageSize` (default 20, min 1, max 100)
- `isEnabled` (optional bool)
- `search` (optional string — matches Id exactly or Name/Email contains, case-insensitive)

POST body:

```json
{
  "name": "Jane Smith",
  "email": "jane.smith@acme.com",
  "password": "P@ssw0rd!"
}
```

`AdviserVm`: `{ "id": 12, "name": "Jane Smith", "email": "jane.smith@acme.com", "isEnabled": true }`

PUT is partial: omitted `name` / `isEnabled` stay unchanged. Supplying neither is 400. Re-enable is `PUT` with `isEnabled: true` and has no extra preconditions.

Cross-tenant ids return **404** (never 403). TenantId is taken from the JWT, not the request body.

See feature spec: [advisers](features/advisers.md).

### 5.5 Customers (TenantAdmin + Adviser)

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- |
| GET | `/customers` | TenantAdmin, Adviser | 200 + paginated list | 400, 401, 403 | List with pagination, `isEnabled` filter, `search` (Id, Name or Email). Advisers see only their own Customers. |
| GET | `/customers/{id}` | TenantAdmin, Adviser | 200 + CustomerVm | 401, 403, 404 | Single Customer (visibility-scoped; out-of-scope and non-Customer ids are 404). Includes assigned Adviser id and name. No Account overview in this slice. |
| POST | `/customers` | TenantAdmin, Adviser | 201 + `{ "id": n }` | 400, 401, 403 | Create Domain User only (no Identity login). Email globally unique. `AdviserId` required. |
| PUT | `/customers/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled and/or reassign AdviserId. Route `{id}` must match body `id`. |
| DELETE | `/customers/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Soft-disable (`IsEnabled = false`). No Account-existence check in this slice. |

Query parameters for list (same shape as Tenants / Advisers):

- `page` (1-based, default 1, min 1)
- `pageSize` (default 20, min 1, max 100)
- `isEnabled` (optional bool)
- `search` (optional string — matches Id exactly or Name/Email contains, case-insensitive)

POST body:

```json
{
  "name": "Zhang San",
  "email": "zhangsan@example.com",
  "adviserId": 12
}
```

`CustomerVm`: `{ "id": 42, "name": "Zhang San", "email": "zhangsan@example.com", "isEnabled": true, "adviserId": 12, "adviserName": "Jane Smith" }`

PUT is partial: omitted `name` / `isEnabled` / `adviserId` stay unchanged. Supplying none is 400. Re-enable is `PUT` with `isEnabled: true` and has no extra preconditions.

On create and reassignment, `adviserId` must reference an enabled Adviser in the same tenant. An Adviser caller may only set `adviserId` to their own Domain User id (400 otherwise). Out-of-scope or cross-tenant Customer ids return **404** (never 403). TenantId is taken from the JWT, not the request body.

See feature spec: [customers](features/customers.md).

### 5.6 Accounts

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

### 5.7 Holdings (nested)

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

### 5.8 Transactions

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

### 5.9 Dashboard

| Method | Route | Description |
| --- | --- | --- |
| GET | `/dashboard/net-worth` | Net worth for the caller’s visible scope |
| GET | `/dashboard/net-worth?customerId={id}` | Net worth for a specific Customer (must be visible to the caller) |
| GET | `/dashboard/allocation` | Asset allocation by AccountType |

Calculation rules follow the domain model (Closed accounts excluded, Credit treated as liability, Brokerage/Property use CostBasis in MVP).

### 5.10 Audit Logs

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
- API versioning
- Production CORS tightening
- Data-archival / clean-up APIs (future maintenance scripts only)
- Password reset / forgot-password flow
- Refresh tokens
- Third-party / social login

## 9. Confirmed decisions

| Topic | Decision |
| --- | --- |
| SystemAdmin scope | Option A — manage Tenants and TenantAdmins only; business data invisible |
| Holdings routes | Nested under `/accounts/{accountId}/holdings` |
| Account close | `Status = Closed`, permanent, no re-activation |
| Customer / Adviser removal | Soft disable (`IsEnabled = false`) + mandatory reassignment for Advisers |
| Net Worth | Supports optional `customerId` filter |
| Auth routes | Custom `/auth/login` + `/auth/logout` (not raw MapIdentityApi surface) |
| Current user | `/users/me` (GET + PUT profile) and `/users/me/password` |
| Customer login | Explicitly rejected with **403** |
| Transaction create response | Return only success + id; client re-fetches Holding if needed |
| Future clean-up | Possible offline script that moves Closed/Disabled rows to archive tables |

## 10. Changelog

| Date | Change |
| --- | --- |
| 2026-08-24 | Tenant Admins slice: `/tenant-admins` CRUD + soft-disable for SystemAdmin. Paginated list (Id/Name/Email search + optional `tenantId`), TenantAdminVm with tenant summary, transactional create with Identity user, create rejects disabled Tenant, last-admin disable allowed. Linked to tenant-admins feature spec. |
| 2026-08-23 | Customers slice: `/customers` CRUD + soft-disable for TenantAdmin and Adviser. Paginated list (Id/Name/Email search), CustomerVm with adviser summary, Domain-only create (no Identity), Adviser self-assignment and 404 visibility scoping. Linked to customers feature spec. |
| 2026-08-23 | Advisers slice: `/advisers` CRUD + soft-disable for TenantAdmin. Paginated list (Id/Name/Email search), AdviserVm, transactional create with Identity user, DELETE customer-reassignment guard. Linked to advisers feature spec. |
| 2026-08-22 | Tenants slice: `/tenants` CRUD for SystemAdmin, pagination shape, TenantVm, partial PUT. Linked to tenants feature spec. |
| 2026-08-21 | Auth surface finalised: custom `/auth` routes, introduced `/users/me` + password endpoint, Customer login → 403. Reversed earlier “no /me” decision. Linked to identity-auth feature spec. |
| 2026-08-20 | Full MVP design written from function-plan, domain-model, database-design and confirmed discussion points (soft-disable, nested holdings, SystemAdmin Option A, irreversible Account close, etc.) |
| 2026-08-16 | Template created; starter endpoint groups listed |
