---
title: "Holdings"
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
  - ../features/accounts.md
  - ../adr/0004-money-as-decimal-with-currency.md
  - ../adr/0005-shared-database-tenantid-isolation.md
  - ../adr/0007-baseentity-primary-key-int.md
---

# Holdings

Tenant-scoped vertical slice for managing Holdings that belong to an Account. Holdings are nested entities inside the Account aggregate. TenantAdmin can manage Holdings on any Account inside their Tenant; Advisers can only manage Holdings on Accounts belonging to their own Customers. A Holding records a position (Instrument + Quantity + CostBasis). Closed Accounts reject all Holding writes. Direct CRUD is provided in this slice; automatic Quantity / average-cost adjustments driven by Transactions are deferred to the Transactions feature.

## 1. Summary

TenantAdmin and Adviser can create, list, view, update and delete Holdings under a visible Account. Routes are nested under `/accounts/{accountId}/holdings`. Currency of CostBasis is forced to match the parent Account and cannot be changed. Quantity may be zero. Account must be Active for any write. Visibility follows the parent Account exactly (same TenantAdmin / Adviser scoping rules already locked in Accounts). No pagination or search on the nested list. Physical delete is allowed in this slice; the guard that prevents deleting a Holding that still has historical Transactions will be added together with the Transactions feature.

## 2. Scope

**In**

- Create Holding under an Account (Instrument.Name required, Symbol optional, Quantity ≥ 0, CostBasis required and currency must equal Account.Currency)
- List all Holdings of a given Account (simple full list, no pagination, no search)
- Get a single Holding by Id (scoped)
- Update Holding (partial: Instrument and/or Quantity and/or CostBasis.Amount; Currency is immutable)
- Delete Holding (physical delete)
- Roles allowed: TenantAdmin and Adviser
- Visibility: identical to Account (TenantAdmin = whole tenant; Adviser = only own Customers’ Accounts)
- Domain entity + EF configuration + commands/queries + endpoints + tests
- Note in docs that the “cannot delete Holding with historical Transactions” rule will be enforced when the Transactions feature ships

**Out**

- Top-level `/holdings` routes
- Pagination or search on the nested list
- Changing CostBasis.Currency after creation
- Soft-delete of Holdings
- Any automatic adjustment of Quantity / CostBasis by Transactions (next feature)
- Forced clearing of Holdings when an Account is closed (already decided in Accounts)
- UI pages (API only for this slice)
- SystemAdmin managing business Holdings (Option A remains locked)
- Market prices, current-value calculation, or historical cost lots

## 3. User stories

1. As a TenantAdmin I want to create a Holding under any Account in my Tenant so that the firm can record a client’s current position.
2. As an Adviser I want to create Holdings only under Accounts that belong to Customers assigned to me.
3. As a TenantAdmin or Adviser I want to list every Holding that belongs to a given Account.
4. As a TenantAdmin or Adviser I want to view a single Holding’s details.
5. As a TenantAdmin or Adviser I want to update the instrument name/symbol, quantity or cost basis of a Holding.
6. As a TenantAdmin or Adviser I want to delete a Holding that is no longer needed.
7. As the system I must return 404 (never 403) when a caller requests an Account or Holding outside their visibility.
8. As the system I must reject all Holding writes against a Closed Account.
9. As the system I must keep CostBasis.Currency identical to the parent Account.Currency at all times.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only users with `Role = TenantAdmin` or `Role = Adviser` may access any Holding endpoint. |
| R2 | All operations are strictly scoped to the caller’s visibility via the parent Account. TenantAdmin sees every Holding under Accounts in the Tenant; Adviser sees only Holdings under Accounts whose Customer has `AdviserId` equal to the caller’s Domain User Id. Cross-scope or cross-tenant ids return 404. |
| R3 | The target Account must exist, be visible to the caller, and have `Status = Active`. Any write (create / update / delete) against a Closed Account returns 400. |
| R4 | On create, `CostBasis.Currency` must equal the parent Account’s `Currency`. Mismatch is 400. |
| R5 | `Quantity` must be ≥ 0 (zero is allowed). Negative values are rejected. |
| R6 | `Instrument.Name` is required and must not be null or whitespace. `Symbol` is optional. |
| R7 | PUT is partial: any combination of `instrument`, `quantity` and/or `costBasis.amount` may be supplied. Supplying none of them is 400. `CostBasis.Currency` cannot be changed and is ignored / rejected if present. |
| R8 | DELETE is a physical delete in this slice. The invariant “a Holding that still has historical Transactions cannot be deleted” will be enforced when the Transactions feature is implemented (documented now, delivered together with Transactions). |
| R9 | List returns the full set of Holdings for the Account. No pagination, no search, no extra filters. |
| R10 | An Adviser caller may only create / update / delete Holdings that belong to Accounts of their own Customers. |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| Holding | Entity (inside Account aggregate) | Introduced in this slice. |
| Instrument | Value object | Name required, Symbol optional. |
| Money | Value object | Used for CostBasis; currency must match Account. |

Fields (aligned with [domain-model.md](../domain-model.md)):

- `Id` : int (identity)
- `TenantId` : int (required, copied from Account)
- `AccountId` : int (required)
- `Instrument` : Instrument (Name + optional Symbol)
- `Quantity` : decimal (≥ 0)
- `CostBasis` : Money (total cost of the current quantity; currency = Account.Currency)
- Audit columns

Invariants:

- Quantity never goes negative.
- CostBasis.Currency is always identical to the owning Account’s Currency and cannot be changed after creation.
- Writes are refused while the parent Account has Status = Closed.
- Prefer keeping a zero-quantity Holding rather than deleting one that has historical Transactions (guard will be added in the Transactions feature).

Domain events (raise if cheap):

- Raised via the Account aggregate (`HoldingChanged`) or dedicated `HoldingCreated` / `HoldingDeleted` if useful for the current slice.

Update [domain-model.md](../domain-model.md) only if further clarification is needed (Holding is already described).

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| Holdings | add | See below |

Columns (aligned with [database-design.md](../database-design.md)):

- `Id` int identity PK
- `TenantId` int not null (**FK → Tenants**, ON DELETE RESTRICT)
- `AccountId` int not null (**FK → Accounts**, ON DELETE CASCADE)
- `Instrument_Name` nvarchar(200) not null
- `Instrument_Symbol` nvarchar(50) null
- `Quantity` decimal(18,8) not null
- `CostBasis_Amount` decimal(18,4) not null
- `CostBasis_Currency` char(3) not null
- audit columns (`Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`)

Indexes:

- `(TenantId, AccountId)`
- `(AccountId)`

Notes:

- Instrument and Money are mapped as owned types / complex properties.
- EnsureCreated is still used; no EF migration in this slice unless the project switches later.
- Account → Holdings is CASCADE because Holdings belong exclusively to the Account aggregate. Close is a status change, not a SQL delete, so historical rows are retained.

Update [database-design.md](../database-design.md) in the same change if any column precision or index needs tightening.

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | CreateHolding | int (new Id) | AccountId must exist, be Active and visible; Name required; Quantity ≥ 0; CostBasis.Currency matches Account |
| Command | UpdateHolding | — | At least one of instrument / quantity / costBasis.amount required; Quantity ≥ 0; Currency forbidden |
| Command | DeleteHolding | — | Holding must exist and belong to a visible Active Account |
| Query | GetHoldingsByAccount | List\<HoldingVm\> | Account must be visible (404 otherwise) |
| Query | GetHoldingById | HoldingVm (404 if missing or out of scope) | |

Scaffold from `src/Application`:

```bash
dotnet new ca-usecase --name CreateHolding --feature-name Holdings --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateHolding --feature-name Holdings --usecase-type command --return-type unit
dotnet new ca-usecase --name DeleteHolding --feature-name Holdings --usecase-type command --return-type unit
dotnet new ca-usecase --name GetHoldingsByAccount --feature-name Holdings --usecase-type query --return-type List<HoldingVm>
dotnet new ca-usecase --name GetHoldingById --feature-name Holdings --usecase-type query --return-type HoldingVm
```

## 8. API

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/accounts/{accountId}/holdings` | TenantAdmin, Adviser | 200 + list | 401, 403, 404 | All holdings of the account (full list). |
| GET | `/accounts/{accountId}/holdings/{id}` | TenantAdmin, Adviser | 200 + HoldingVm | 401, 403, 404 | Single holding (visibility-scoped). |
| POST | `/accounts/{accountId}/holdings` | TenantAdmin, Adviser | 201 + `{ "id": n }` | 400, 401, 403, 404 | Create holding. |
| PUT | `/accounts/{accountId}/holdings/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Partial update (instrument / quantity / costBasis.amount). |
| DELETE | `/accounts/{accountId}/holdings/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Physical delete. |

Request body for POST:

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

Request body for PUT (partial):

```json
{
  "instrument": {
    "name": "Apple Inc.",
    "symbol": "AAPL"
  },
  "quantity": 120,
  "costBasis": {
    "amount": 19200.00
  }
}
```

Supplying none of the updatable fields is 400. `costBasis.currency` is ignored / rejected if present.

Suggested `HoldingVm` shape:

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

Update [api-design.md](../api-design.md) in the same change.

## 9. UI

None — API only for this slice. Account detail pages in the Adviser Portal can surface the Holdings list / create / edit later once the API is stable.

## 10. Tests

| Project | Cases |
| --- | --- |
| Domain.UnitTests | Holding invariants (Quantity ≥ 0, CostBasis currency matches Account, Closed Account rejects writes) |
| Application.UnitTests | Create rejects Closed / invisible Account, currency mismatch, negative Quantity; Update rejects empty body and currency change; Adviser cannot target another Adviser’s Customer Account; non-allowed roles rejected by policy |
| Application.FunctionalTests | TenantAdmin happy path: create → list → get → update → delete under an Active Account; Adviser can only see and manage Holdings of own Customers’ Accounts; Closed Account write returns 400; out-of-scope Account/Holding returns 404 |
| Web / Integration | 403 for SystemAdmin / anonymous; 404 for cross-tenant or other-Adviser Holding; 400 when Currency is supplied on PUT or when Quantity is negative |

## 11. Rollout

- [x] Feature spec accepted
- [x] Domain `Holding` entity + `Instrument` value object + invariants
- [x] EF configuration (owned types, FKs, indexes) + EnsureCreated / seed updates if needed
- [x] Commands / queries / validators
- [x] Endpoints under `/accounts/{accountId}/holdings`
- [x] Tests
- [x] Parent docs updated (domain-model if needed, database-design, api-design, function-plan, features/README)
- [x] Note left for Transactions feature: add “cannot delete Holding that still has historical Transactions” guard

## 12. Open questions

- None remaining after 2026-08-24 decisions.
  - DELETE of a Holding that has historical Transactions is **not** allowed; the guard will be implemented together with the Transactions feature (documented now).
  - Nested list has **no** search / pagination.
  - CostBasis.Currency is immutable and must always equal Account.Currency.
  - Quantity = 0 is allowed on create.

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-24 | Implemented. Nested `/accounts/{accountId}/holdings` for TenantAdmin + Adviser. Quantity ≥ 0 (incl. zero). CostBasis.Currency immutable and must match Account. Physical DELETE allowed; Transactions slice will add the historical-Tx guard. Account → Holdings FK is CASCADE (close is not a SQL delete). Sample Apple holding seeded. |
| 2026-08-24 | Created from discussion. Locked: nested routes only, no list search/pagination, Currency immutable, Quantity ≥ 0 (incl. zero), physical DELETE allowed now with future Transactions guard documented, visibility identical to Accounts. |
