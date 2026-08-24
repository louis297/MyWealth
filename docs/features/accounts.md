---
title: "Accounts"
status: accepted
owner: ""
last_updated: 2026-08-24
related:
  - ../function-plan.md
  - ../domain-model.md
  - ../database-design.md
  - ../api-design.md
  - ../features/identity-auth.md
  - ../features/tenants.md
  - ../features/tenant-admins.md
  - ../features/advisers.md
  - ../features/customers.md
  - ../adr/0004-money-as-decimal-with-currency.md
  - ../adr/0005-shared-database-tenantid-isolation.md
  - ../adr/0007-baseentity-primary-key-int.md
---

# Accounts

Tenant-scoped vertical slice for managing Customer accounts. TenantAdmin can manage every Account inside their Tenant; Advisers can only manage Accounts belonging to their own Customers. An Account is the aggregate root that will later own Holdings and Transactions. Closing an Account is permanent (`Status = Closed`); existing Holdings and Transactions are retained for history and are **not** required to be cleared. Currency is fixed at creation. AccountType follows the values already defined in the domain model.

## 1. Summary

TenantAdmin and Adviser can create Accounts for Customers in their visibility scope, list them (with pagination, status filter, optional customerId filter and search), view a single Account, update Name and/or Type, and permanently close an Account. Closed accounts remain visible for historical queries but reject all future writes (Holdings / Transactions) and are excluded from Net Worth. Closing does **not** force any Holding or Transaction to be cleared.

## 2. Scope

**In**

- Create Account (CustomerId, Name, Type, Currency required; Status defaults to Active)
  - TenantId is copied from the target Customer
  - Currency becomes immutable after insert
- List Accounts with:
  - pagination
  - filter by Status
  - optional `customerId` filter
  - search by Id or Name (case-insensitive)
  - automatic visibility scoping (TenantAdmin = whole tenant; Adviser = only own Customers’ Accounts)
- Get Account by Id (scoped; out-of-scope returns 404)
- Update Account (Name and/or Type only)
- Close Account via `POST /accounts/{id}/close` (sets Status = Closed, irreversible)
- Roles allowed: TenantAdmin and Adviser
- Domain aggregate root + EF configuration + commands/queries + endpoints + tests

**Out**

- Physical deletion of Accounts
- Re-opening a Closed Account
- Changing Currency after create
- Changing the owning Customer
- Any check or forced clearing of Holdings / Transactions on close (explicitly not required)
- Holding or Transaction operations (next features)
- UI pages (API only for this slice)
- SystemAdmin managing business Accounts (Option A remains locked)
- Net Worth calculation (Dashboard feature)

## 3. User stories

1. As a TenantAdmin I want to create an Account for any Customer in my Tenant so that the firm can start recording that client’s assets.
2. As an Adviser I want to create an Account only for Customers assigned to me.
3. As a TenantAdmin or Adviser I want to list Accounts with pagination, status filter, optional customer filter and name search so that I can find them quickly.
4. As a TenantAdmin or Adviser I want to view a single Account’s details.
5. As a TenantAdmin or Adviser I want to rename an Account or change its Type.
6. As a TenantAdmin or Adviser I want to permanently close an Account so that no further holdings or transactions can be posted against it, while still being able to view its history.
7. As the system I must return 404 (never 403) when a caller requests an Account outside their visibility.
8. As the system I must allow an Account to be closed even when it still has Holdings or Transactions.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only users with `Role = TenantAdmin` or `Role = Adviser` may access any Account endpoint. |
| R2 | All operations are strictly scoped to the caller’s visibility. TenantAdmin sees every Account in the Tenant; Adviser sees only Accounts whose Customer has `AdviserId` equal to the caller’s Domain User Id. Cross-scope or cross-tenant ids return 404. |
| R3 | On create, `CustomerId` must reference an existing User with `Role = Customer`, the same `TenantId`, and `IsEnabled = true`. |
| R4 | `Currency` is required on create and becomes immutable. |
| R5 | `Status` may only transition from `Active` → `Closed`. The transition is irreversible. |
| R6 | Closing an Account does **not** require, check or clear any existing Holdings or Transactions. Historical data is retained. |
| R7 | Once `Status = Closed`, all subsequent write operations against the Account (create/update/delete Holding, create Transaction) must be rejected. This invariant is enforced by the Account aggregate and will be checked by later features. |
| R8 | List endpoint supports pagination, optional `status`, optional `customerId`, and `search` (Id exact or Name contains, case-insensitive). |
| R9 | PUT is partial: any combination of `name` and/or `type` may be supplied. Supplying neither is 400. Currency, CustomerId and Status cannot be changed via PUT. |
| R10 | Close is performed by a dedicated `POST /accounts/{id}/close` action (not via PUT). |
| R11 | `AccountType` uses the values already defined in the domain model: Bank, Cash, Brokerage, Property, Credit, Other. Credit accounts are treated as liabilities for future Net Worth calculations. |
| R12 | An Adviser caller may only create or close Accounts that belong to their own Customers. |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| Account | Aggregate root | Introduced in this slice. Will later own Holdings and Transactions. |

Fields (aligned with [domain-model.md](../domain-model.md)):

- `Id` : int (identity)
- `TenantId` : int (required, copied from Customer)
- `CustomerId` : int (required)
- `Name` : string (required)
- `Type` : AccountType (Bank / Cash / Brokerage / Property / Credit / Other)
- `Status` : AccountStatus (Active / Closed)
- `Currency` : string (ISO 4217, required, immutable after create)
- Audit columns

Invariants:

- Currency never changes after creation.
- Status may only move Active → Closed; the reverse is forbidden.
- Status = Closed ⇒ no new Holdings and no new Transactions (enforced by aggregate and by later features).
- CustomerId must reference a User with Role = Customer, same TenantId and IsEnabled = true at creation time.
- Name must not be null or whitespace.
- Type must be a valid AccountType value.
- Every Money value that later appears on child Holdings / Transactions must use this Account’s Currency.

Domain events (raise if cheap):

- `AccountOpened` (on create)
- `AccountClosed` (on close)

Update [domain-model.md](../domain-model.md) only if further clarification is needed (the Account aggregate is already described).

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| Accounts | add | See below |

Columns (aligned with [database-design.md](../database-design.md)):

- `Id` int identity PK
- `TenantId` int not null (**FK → Tenants**, ON DELETE RESTRICT)
- `CustomerId` int not null (**FK → Users**, ON DELETE RESTRICT)
- `Name` nvarchar(200) not null
- `Type` int not null (AccountType enum)
- `Status` int not null (AccountStatus enum, default Active)
- `Currency` char(3) not null
- audit columns (`Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`)

Indexes:

- `(TenantId, CustomerId)`
- `(CustomerId)`
- `(TenantId, Status)` (useful for filtered lists)

Notes:

- Currency is stored as a fixed char(3); no separate Currencies lookup table in MVP.
- EnsureCreated is still used; no EF migration in this slice unless the project switches later.
- No cascade delete from Customer → Accounts in this slice (Accounts outlive a soft-disabled Customer for history).

Update [database-design.md](../database-design.md) in the same change if any column precision or index needs tightening.

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | CreateAccount | int (new Id) | CustomerId, Name, Type, Currency required; Customer must exist, be enabled, Role = Customer, same Tenant; Currency valid ISO code |
| Command | UpdateAccount | — | Name and/or Type optional but at least one required; Status / Currency / CustomerId forbidden |
| Command | CloseAccount | — | Account must be Active; already-Closed is 400 |
| Query | GetAccounts | paginated list | page, pageSize, status?, customerId?, search? + automatic role-based visibility filter |
| Query | GetAccountById | AccountVm (404 if missing or out of scope) | |

Scaffold from `src/Application`:

```bash
dotnet new ca-usecase --name CreateAccount --feature-name Accounts --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateAccount --feature-name Accounts --usecase-type command --return-type unit
dotnet new ca-usecase --name CloseAccount --feature-name Accounts --usecase-type command --return-type unit
dotnet new ca-usecase --name GetAccounts --feature-name Accounts --usecase-type query --return-type PaginatedList<AccountVm>
dotnet new ca-usecase --name GetAccountById --feature-name Accounts --usecase-type query --return-type AccountVm
```

## 8. API

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/accounts` | TenantAdmin, Adviser | 200 + paginated list | 401, 403 | List with pagination, status filter, optional customerId, search. Adviser sees only own Customers’ Accounts. |
| GET | `/accounts/{id}` | TenantAdmin, Adviser | 200 + AccountVm | 401, 403, 404 | Single Account (visibility-scoped). |
| POST | `/accounts` | TenantAdmin, Adviser | 201 + `{ "id": n }` | 400, 401, 403 | Create Account. |
| PUT | `/accounts/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Update Name and/or Type. |
| POST | `/accounts/{id}/close` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Permanently close (Status = Closed). Irreversible. |

Query parameters for list:

- `page` (1-based, default 1)
- `pageSize` (default 20, max 100)
- `status` (optional: `Active` \| `Closed`)
- `customerId` (optional int)
- `search` (optional string – matches Id exactly or Name contains, case-insensitive)

Request body for POST:

```json
{
  "customerId": 42,
  "name": "Primary Brokerage",
  "type": "Brokerage",
  "currency": "NZD"
}
```

Request body for PUT (partial):

```json
{
  "name": "Main Brokerage Account",
  "type": "Brokerage"
}
```

Supplying neither `name` nor `type` is 400. Currency, customerId and status are ignored / rejected if present.

`POST /accounts/{id}/close` has an empty body.

Suggested `AccountVm` shape:

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

Update [api-design.md](../api-design.md) in the same change.

## 9. UI

None — API only for this slice. Account list / create / edit / close pages in the Adviser Portal can be added later once the API is stable.

## 10. Tests

| Project | Cases |
| --- | --- |
| Domain.UnitTests | Account invariants (Currency immutable, Status one-way transition, valid Type, Name required) |
| Application.UnitTests | Create rejects invalid / disabled / wrong-role Customer; Close rejects already-Closed; non-allowed roles rejected by policy; Adviser cannot target another Adviser’s Customer |
| Application.FunctionalTests | TenantAdmin happy path: create → list (filter by status / customerId / search / page) → get → update Name/Type → close; Adviser can only see and manage own Customers’ Accounts; closed Account remains visible in list/detail |
| Web / Integration | 403 for SystemAdmin / anonymous; 404 for cross-tenant or other-Adviser Account; 400 on close of already-Closed Account; 400 when Currency is supplied on PUT |

## 11. Rollout

- [x] Feature spec accepted
- [x] Domain `Account` entity + invariants + optional events
- [x] EF configuration (FKs, indexes) + EnsureCreated / seed updates if needed
- [x] Commands / queries / validators
- [x] Endpoints under `/accounts` (+ `/close`)
- [x] Tests
- [x] Parent docs updated (domain-model if needed, database-design, api-design, function-plan, features/README)

## 12. Open questions

- None remaining after 2026-08-24 decisions.
  - Close does **not** force clearing of Holdings / Transactions
  - AccountType values taken from domain-model
  - Optional `customerId` filter on list is included (low cost, already sketched in api-design)
  - Close action is `POST /accounts/{id}/close`
  - PUT may change Type (as well as Name)
  - Status values are `Active` / `Closed` (domain-model wording)

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-24 | Implemented. TenantAdmin + Adviser CRUD under `/accounts`. Close is `POST /accounts/{id}/close` (irreversible). Currency immutable. PUT Name and/or Type (forbidden fields rejected). Visibility 404 scoping. Customer disable now refused while Active accounts remain. Sample Brokerage account seeded. |
| 2026-08-24 | Created from discussion. Locked: no forced clear on close, AccountType from domain-model, customerId list filter, POST /close, PUT may change Type, Status = Active/Closed. |
