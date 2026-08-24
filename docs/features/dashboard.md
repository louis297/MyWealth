---
title: "Dashboard"
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
  - ../features/holdings.md
  - ../features/transactions.md
  - ../adr/0004-money-as-decimal-with-currency.md
  - ../adr/0005-shared-database-tenantid-isolation.md
  - ../adr/0007-baseentity-primary-key-int.md
---

# Dashboard

Tenant-scoped vertical slice that exposes two pure read-model endpoints for current Net Worth and asset allocation. No new aggregates, tables or migrations. TenantAdmin sees the whole Tenant; Advisers are restricted to their own Customers. Closed accounts are always excluded. Multi-currency results are returned as an array grouped by currency; no FX conversion is performed. Account Performance Overview is explicitly out of scope for this slice.

## 1. Summary

TenantAdmin and Adviser can retrieve the current Net Worth (Assets − Liabilities) and the asset allocation broken down by AccountType for their visible scope. Both endpoints optionally accept a `customerId` filter. Results are always multi-currency aware (one item per currency present). Calculation rules follow the domain model: Closed accounts are excluded, Credit is treated as a liability, Bank/Cash/Other use the signed sum of Transactions, Brokerage/Property use the sum of Holdings’ CostBasis. No pagination, sorting or search — these are aggregate results, not lists.

## 2. Scope

**In**

- `GET /dashboard/net-worth` (optional `?customerId={id}`)
- `GET /dashboard/allocation` (optional `?customerId={id}`)
- Roles allowed: TenantAdmin and Adviser
- Visibility identical to Accounts / Holdings / Transactions:
  - TenantAdmin → entire Tenant
  - Adviser → only Customers where `AdviserId` equals the caller’s Domain User Id
  - Out-of-scope or cross-tenant `customerId` returns 404
- Value calculation (domain-model §4.3):
  - Closed accounts are completely excluded
  - Credit accounts are treated as liabilities
  - Bank / Cash / Other → signed sum of the Account’s Transactions
  - Brokerage / Property → sum of Holdings’ `CostBasis.Amount`
- Signed-sum sign convention (locked):
  - Positive contribution (`+Amount`): `TransferIn`, `Dividend`, `Interest`, `Sell`
  - Negative contribution (`-Amount`): `TransferOut`, `Buy`
  - Same sign rules apply to Credit accounts (positive value = outstanding liability)
- Multi-currency: results are returned as an array, one entry per currency. No automatic conversion.
- Empty visible data → empty array
- Pure Application-layer queries + view models; no Domain events, no new tables, no migration

**Out**

- Historical Net Worth snapshots / time series
- Account Performance Overview / returns / recent changes (explicitly deferred)
- Market prices, live valuation, automatic FX conversion
- Any write operations, materialised views or cache tables
- SystemAdmin access to business data (Option A remains locked)
- UI pages (API only for this slice)
- Pagination, sorting or free-text search on either endpoint
- Custom asset-class taxonomy (allocation is by `AccountType` only)

## 3. User stories

1. As a TenantAdmin I want to see the current Net Worth and asset allocation for the whole Tenant so that I can monitor the firm’s overall position.
2. As an Adviser I want to see Net Worth and allocation only for Customers assigned to me.
3. As a TenantAdmin or Adviser I want to optionally filter both views to a single Customer that is visible to me.
4. As the system I must exclude every Closed account from the calculations.
5. As the system I must treat Credit accounts as liabilities and apply the locked signed-sum rules.
6. As the system I must return multi-currency results as an array grouped by currency and never perform FX conversion.
7. As the system I must return an empty array when there is no visible Active data.
8. As the system I must return 404 (never 403) when the supplied `customerId` is outside the caller’s visibility.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only users with `Role = TenantAdmin` or `Role = Adviser` may call the endpoints. |
| R2 | Visibility is identical to the Account resource. TenantAdmin sees the whole Tenant; Adviser sees only Accounts belonging to Customers where `AdviserId` equals the caller’s Domain User Id. A `customerId` that is missing, belongs to another Tenant, or is not visible to the caller returns 404. |
| R3 | Only Accounts with `Status = Active` participate in the calculation. Closed accounts are excluded. |
| R4 | Account value by type: |
|    | - Bank / Cash / Other → signed sum of that Account’s Transactions |
|    | - Credit → signed sum of that Account’s Transactions (treated as liability) |
|    | - Brokerage / Property → sum of the Account’s Holdings’ `CostBasis.Amount` |
| R5 | Signed-sum sign convention (locked): |
|    | - `+Amount` for `TransferIn`, `Dividend`, `Interest`, `Sell` |
|    | - `-Amount` for `TransferOut`, `Buy` |
|    | The same mapping is used for Credit accounts. |
| R6 | Net Worth = Σ(non-liability account values) − Σ(liability account values), computed independently per currency. |
| R7 | Asset allocation groups the same account values by `AccountType` (and by currency). |
| R8 | Results are always multi-currency arrays. No FX conversion is performed. |
| R9 | No pagination, sorting or search parameters. Both endpoints return aggregate results. |
| R10 | When there are no visible Active accounts (or no data in a given currency), the corresponding array is empty. |
| R11 | All monetary amounts are returned with their currency (Money shape). |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| (none) | — | Pure read model. No new aggregates, entities, value objects or domain events. |

Invariants:

- Calculation rules are enforced only in the Application query handlers (they mirror the description already present in domain-model §4.3).
- No Domain events are raised.

Update [domain-model.md](../domain-model.md) only if further clarification of the signed-sum mapping is required; the high-level rules already exist.

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| (none) | — | No schema change |

No migration. Queries read existing `Accounts`, `Holdings` and `Transactions` tables (with the usual TenantId global filters and application-level visibility checks).

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Query | GetNetWorth | `NetWorthVm` | Optional `customerId` must be visible to the caller (otherwise the handler returns 404) |
| Query | GetAssetAllocation | `AssetAllocationVm` | Same visibility rule for optional `customerId` |

Suggested view-model shapes:

```text
NetWorthVm
  items: NetWorthItemVm[]

NetWorthItemVm
  currency: string          // ISO 4217
  assets: decimal
  liabilities: decimal
  net: decimal              // assets - liabilities

AssetAllocationVm
  items: AllocationItemVm[]

AllocationItemVm
  accountType: string       // AccountType enum name
  currency: string
  value: decimal
  // percentage is optional and can be computed client-side or added later
```

Scaffold:

```bash
dotnet new ca-usecase -n GetNetWorth -fn Dashboard -ut query -rt NetWorthVm
dotnet new ca-usecase -n GetAssetAllocation -fn Dashboard -ut query -rt AssetAllocationVm
```

## 8. API

| Method | Route | Auth | Success | Errors |
| --- | --- | --- | --- | --- |
| GET | `/dashboard/net-worth` | TenantAdmin, Adviser | 200 + NetWorthVm | 401, 403 |
| GET | `/dashboard/net-worth?customerId={id}` | TenantAdmin, Adviser | 200 + NetWorthVm | 401, 403, 404 |
| GET | `/dashboard/allocation` | TenantAdmin, Adviser | 200 + AssetAllocationVm | 401, 403 |
| GET | `/dashboard/allocation?customerId={id}` | TenantAdmin, Adviser | 200 + AssetAllocationVm | 401, 403, 404 |

Update [api-design.md](../api-design.md) in the same change (add optional `customerId` on allocation and document the multi-currency array shape).

## 9. UI

None — API only for this slice.

(Adviser Portal Dashboard home page can consume these two endpoints later.)

## 10. Tests

| Project | Cases |
| --- | --- |
| Domain.UnitTests | (none — no new Domain types) |
| Application.UnitTests | Visibility (TenantAdmin vs Adviser vs foreign customerId → 404); Closed accounts excluded; Credit treated as liability; signed-sum signs for all six TransactionTypes; Brokerage/Property use CostBasis; multi-currency grouping; empty result → empty array |
| Application.FunctionalTests | Happy path for TenantAdmin (whole tenant) and Adviser (own customers only); customerId filter; mix of Bank + Brokerage + Credit accounts; multi-currency data; empty tenant returns empty arrays |
| Web / Integration | 403 for SystemAdmin / anonymous; 404 for invisible customerId |

## 11. Rollout

- [x] Feature spec accepted
- [x] (no Domain types)
- [x] (no EF configuration / migration)
- [x] GetNetWorth + GetAssetAllocation queries + validators
- [x] Endpoints under `/dashboard`
- [x] Tests
- [x] Parent docs updated (api-design, features/README, function-plan if needed)

## 12. Open questions

- None remaining after 2026-08-24 decisions.
  - Signed-sum mapping locked (R5).
  - Both endpoints support optional `customerId`.
  - Multi-currency results returned as currency-grouped arrays.
  - Empty data returns empty arrays.
  - Account Performance Overview is out of scope for this slice.

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-24 | Implemented. `GET /dashboard/net-worth` and `GET /dashboard/allocation` for TenantAdmin + Adviser. Optional `customerId` (404 if invisible). Multi-currency arrays, empty → empty array, Closed excluded, Credit = liability, signed-sum locked. Pure read model, API only. |
| 2026-08-24 | Created from discussion. Locked: signed-sum signs (TransferIn/Dividend/Interest/Sell = +, TransferOut/Buy = −), allocation also accepts customerId, multi-currency as array, empty → empty array, no Account Performance, pure read model, API only. |
