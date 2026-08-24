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
  - features/advisers.md
  - features/customers.md
  - features/accounts.md
  - features/holdings.md
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
| Tenant Admins | `/tenant-admins` | GET list, GET id, POST, PUT, disable | SystemAdmin only | Creates Domain User + Identity login |
| Advisers | `/advisers` | GET list, GET id, POST, PUT, disable | TenantAdmin | |
| Customers | `/customers` | GET list, GET id, POST, PUT, disable | TenantAdmin, Adviser | Must supply `AdviserId` on create |
| Accounts | `/accounts` | GET list, GET id, POST, PUT, POST close | TenantAdmin, Adviser | Currency immutable after create; close is permanent (`POST /{id}/close`); no forced clear of children |
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

### 5.3 Advisers (TenantAdmin)

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

### 5.4 Customers (TenantAdmin + Adviser)

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- |
| GET | `/customers` | TenantAdmin, Adviser | 200 + paginated list | 400, 401, 403 | List with pagination, `isEnabled` filter, `search` (Id, Name or Email). Advisers see only their own Customers. |
| GET | `/customers/{id}` | TenantAdmin, Adviser | 200 + CustomerVm | 401, 403, 404 | Single Customer (visibility-scoped; out-of-scope and non-Customer ids are 404). Includes assigned Adviser id and name. No Account overview in this slice. |
| POST | `/customers` | TenantAdmin, Adviser | 201 + `{ "id": n }` | 400, 401, 403 | Create Domain User only (no Identity login). Email globally unique. `AdviserId` required. |
| PUT | `/customers/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled and/or reassign AdviserId. Route `{id}` must match body `id`. |
| DELETE | `/customers/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Soft-disable (`IsEnabled = false`). 400 if the Customer still has any Active Account. Closed accounts do not block. |

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

### 5.5 Accounts

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/accounts` | TenantAdmin, Adviser | 200 + paginated list | 401, 403 | List with pagination, `status` filter, optional `customerId`, `search`. Adviser sees only own Customers’ Accounts. |
| GET | `/accounts/{id}` | TenantAdmin, Adviser | 200 + AccountVm | 401, 403, 404 | Single Account (visibility-scoped). |
| POST | `/accounts` | TenantAdmin, Adviser | 201 + `{ "id": n }` | 400, 401, 403 | Create Account. Currency becomes immutable. |
| PUT | `/accounts/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Update Name and/or Type. Currency, CustomerId and Status cannot be changed. |
| POST | `/accounts/{id}/close` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Permanently set `Status = Closed`. Irreversible. No forced clear of Holdings/Transactions. |

Query parameters for list:

- `page` (1-based, default 1)
- `pageSize` (default 20, max 100)
- `status` (optional: `Active` \| `Closed`)
- `customerId` (optional int)
- `search` (optional string – matches Id exactly or Name contains, case-insensitive)

POST body:

```json
{
  "customerId": 42,
  "name": "Primary Brokerage",
  "type": "Brokerage",
  "currency": "NZD"
}
```

PUT is partial: any combination of `name` and/or `type`. Supplying neither is 400.

`POST /accounts/{id}/close` has an empty body.

`AccountVm`:

```json
{
  "id": 17,
  "customerId": 42,
  "customerName": "Zhang San",
  "name": "Primary Brokerage",
  "type": "Brokerage",
  "status": "Active",
  "currency": "NZD"
}
```

Constraints:

- Currency is immutable after create.
- Close is permanent (`Status = Closed`); re-opening is forbidden.
- Closing does **not** require or clear existing Holdings / Transactions (history is retained).
- Closed accounts reject all new Holdings and Transactions and are excluded from Net Worth.
- Cross-scope or cross-tenant ids return **404** (never 403).

See feature spec: [accounts](features/accounts.md).

### 5.6 Holdings (nested)

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/accounts/{accountId}/holdings` | TenantAdmin, Adviser | 200 + list | 401, 403, 404 | All holdings of the account (full list, no pagination / search). |
| GET | `/accounts/{accountId}/holdings/{id}` | TenantAdmin, Adviser | 200 + HoldingVm | 401, 403, 404 | Single holding (visibility-scoped). |
| POST | `/accounts/{accountId}/holdings` | TenantAdmin, Adviser | 201 + `{ "id": n }` | 400, 401, 403, 404 | Create holding. |
| PUT | `/accounts/{accountId}/holdings/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Partial update (instrument / quantity / costBasis.amount). |
| DELETE | `/accounts/{accountId}/holdings/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Physical delete. |

Key fields / rules:

- `Instrument` (Name required, Symbol optional)
- `Quantity` ≥ 0 (zero allowed)
- `CostBasis` (Money) — currency must match the parent Account and is immutable after create
- Parent Account must be `Status = Active` for any write; Closed → 400
- Visibility follows the parent Account exactly (same TenantAdmin / Adviser scoping)
- Physical DELETE is refused when the Holding still has historical Transactions (400)

POST body:

```json
{
  "instrument": {
    "name": "Apple Inc.",
    "symbol": "AAPL"
  },
  "quantity": 100,
  "costBasis": {
    "amount": 18500.00,
    "currency": "NZD"
  }
}
```

PUT is partial: any combination of `instrument`, `quantity` and/or `costBasis.amount`. Supplying none is 400. `costBasis.currency` is ignored / rejected if present.

`HoldingVm`:

```json
{
  "id": 5,
  "accountId": 17,
  "instrument": {
    "name": "Apple Inc.",
    "symbol": "AAPL"
  },
  "quantity": 100,
  "costBasis": {
    "amount": 18500.00,
    "currency": "NZD"
  }
}
```

See feature spec: [holdings](features/holdings.md).

### 5.7 Transactions

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/transactions` | TenantAdmin, Adviser | 200 + paginated list | 401, 403 | List with pagination and filters (accountId, from, to, type). |
| GET | `/transactions/{id}` | TenantAdmin, Adviser | 200 + TransactionVm | 401, 403, 404 | Single transaction (visibility-scoped). |
| POST | `/transactions` | TenantAdmin, Adviser | 201 + `{ "id": n }` | 400, 401, 403, 404 | Create transaction (returns id only). |

Query parameters for list:

- `page` (1-based, default 1)
- `pageSize` (default 20, max 100)
- `accountId` (optional int)
- `from` (optional date – BookedOn ≥)
- `to` (optional date – BookedOn ≤)
- `type` (optional: `Buy` \| `Sell` \| `TransferIn` \| `TransferOut` \| `Dividend` \| `Interest`)

Rules:

- Append-only: no PUT, no DELETE.
- On success return only the new id (`201 Created`). The client re-queries the Holding if it needs the updated position.
- Amount is always positive; direction is determined solely by `Type`.
- Buy / Sell require `HoldingId` + `Quantity` (> 0) and automatically adjust the Holding’s quantity and average cost basis inside the Account aggregate.
- TransferIn / TransferOut / Dividend / Interest are cash-only (`HoldingId` must be null) and do not touch Holdings.
- Cannot post to a Closed account.
- `Amount.Currency` must equal the Account’s currency.
- `BookedOn` may be a future date.
- Visibility follows the parent Account exactly.
- A Holding that still has historical Transactions cannot be physically deleted (guard delivered by this feature).

POST body (Buy example):

```json
{
  "accountId": 17,
  "holdingId": 5,
  "bookedOn": "2026-08-20",
  "type": "Buy",
  "amount": {
    "amount": 18500.00,
    "currency": "NZD"
  },
  "quantity": 100,
  "note": "Initial purchase"
}
```

POST body (Dividend example):

```json
{
  "accountId": 17,
  "bookedOn": "2026-08-20",
  "type": "Dividend",
  "amount": {
    "amount": 120.50,
    "currency": "NZD"
  },
  "note": "Q2 dividend"
}
```

`TransactionVm`:

```json
{
  "id": 42,
  "accountId": 17,
  "holdingId": 5,
  "bookedOn": "2026-08-20",
  "type": "Buy",
  "amount": {
    "amount": 18500.00,
    "currency": "NZD"
  },
  "quantity": 100,
  "note": "Initial purchase"
}
```

For cash-only types `holdingId` and `quantity` are null.

See feature spec: [transactions](features/transactions.md).

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
- API versioning
- Production CORS tightening
- Data-archival / clean-up APIs (future maintenance scripts only)
- Password reset / forgot-password flow
- Refresh tokens
- Third-party / social login

## 9. Confirmed decisions

| Topic | Decision |
| --- | --- |
| SystemAdmin scope | Option A — manage Tenants only; business data invisible |
| Holdings routes | Nested under `/accounts/{accountId}/holdings` |
| Account close | `POST /accounts/{id}/close` → `Status = Closed`, permanent, no re-activation; no forced clear of Holdings/Transactions |
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
| 2026-08-24 | Transactions slice shipped: top-level `/transactions` list/get/create (append-only). Buy/Sell adjust Holding via average cost. Holding DELETE blocked when historical transactions exist. |
| 2026-08-24 | Holdings slice shipped: nested `/accounts/{accountId}/holdings` list/get/create/partial PUT/delete. CostBasis.Currency immutable and must match Account. Closed Account writes 400; reads remain 200. |
| 2026-08-24 | Accounts slice shipped: `/accounts` list/get/create/update + `POST /{id}/close`. Enums serialized as strings. Customer `DELETE` / disable now returns 400 while Active accounts remain. |
| 2026-08-24 | Accounts slice: detailed `/accounts` endpoints (list with status/customerId/search, create, partial PUT Name+Type, dedicated POST close). Currency immutable, close permanent and does not force-clear children. Linked to accounts feature spec. |
| 2026-08-23 | Customers slice: `/customers` CRUD + soft-disable for TenantAdmin and Adviser. Paginated list (Id/Name/Email search), CustomerVm with adviser summary, Domain-only create (no Identity), Adviser self-assignment and 404 visibility scoping. Linked to customers feature spec. |
| 2026-08-23 | Advisers slice: `/advisers` CRUD + soft-disable for TenantAdmin. Paginated list (Id/Name/Email search), AdviserVm, transactional create with Identity user, DELETE customer-reassignment guard. Linked to advisers feature spec. |
| 2026-08-22 | Tenants slice: `/tenants` CRUD for SystemAdmin, pagination shape, TenantVm, partial PUT. Linked to tenants feature spec. |
| 2026-08-21 | Auth surface finalised: custom `/auth` routes, introduced `/users/me` + password endpoint, Customer login → 403. Reversed earlier “no /me” decision. Linked to identity-auth feature spec. |
| 2026-08-20 | Full MVP design written from function-plan, domain-model, database-design and confirmed discussion points (soft-disable, nested holdings, SystemAdmin Option A, irreversible Account close, etc.) |
| 2026-08-16 | Template created; starter endpoint groups listed |
