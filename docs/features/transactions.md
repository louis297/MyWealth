---
title: "Transactions"
status: accepted
owner: ""
last_updated: 2026-08-26
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
  - ../adr/0004-money-as-decimal-with-currency.md
  - ../adr/0005-shared-database-tenantid-isolation.md
  - ../adr/0007-baseentity-primary-key-int.md
---

# Transactions

Tenant-scoped vertical slice for posting and querying Transactions that belong to an Account. Transactions are append-only entities inside the Account aggregate. TenantAdmin can post and view Transactions on any Account inside their Tenant; Advisers are restricted to Accounts belonging to their own Customers. Buy and Sell transactions automatically adjust the related Holding’s Quantity and average cost basis inside the same aggregate boundary. Cash-only types (TransferIn, TransferOut, Dividend, Interest) never touch Holdings. Closed Accounts reject all new Transactions. This slice also delivers the Holding-delete guard that was deferred from the Holdings feature: a Holding that still has historical Transactions cannot be physically deleted.

## 1. Summary

TenantAdmin and Adviser can create Transactions (append-only) and list / view them with pagination and filters. Routes are top-level under `/transactions`. Create returns only the new id; the client must re-query the Holding if it needs the updated position. Amount is always positive; direction is determined solely by `Type`. `BookedOn` may be a future date (to support back-dating / future-dated entries). Visibility follows the parent Account exactly. In the same change we harden `DeleteHolding` so that any Holding referenced by at least one Transaction is protected from deletion.

## 2. Scope

**In**

- Create Transaction (AccountId required)
  - Types: `Buy`, `Sell`, `TransferIn`, `TransferOut`, `Dividend`, `Interest`
  - Buy / Sell require `HoldingId` + `Quantity` (> 0) and atomically adjust the Holding (average-cost method)
  - Cash-only types require `HoldingId` to be null
- List Transactions with pagination and filters:
  - optional `accountId`
  - optional date range on `BookedOn` (`from` / `to`)
  - optional `type`
  - automatic visibility scoping
- Get a single Transaction by Id (scoped)
- Average-cost adjustment of Holding.Quantity and Holding.CostBasis for Buy / Sell, performed inside the Account aggregate
- Holding-delete guard: refuse `DELETE /accounts/{accountId}/holdings/{id}` when any Transaction references that Holding
- Roles allowed: TenantAdmin and Adviser
- Domain entity + EF configuration + commands/queries + endpoints + tests

**Out**

- Update or delete of an existing Transaction (strict append-only in MVP)
- Paired / multi-leg transfers (a Transfer is recorded on one Account only)
- Custom categories or user-defined tags
- Tax-lot accounting, FIFO, LIFO or any method beyond simple average cost
- Returning the updated Holding snapshot in the Create response (client re-queries)
- UI pages (API only for this slice)
- SystemAdmin managing business Transactions (Option A remains locked)
- Negative Amount values (Amount is always positive; Type determines direction)

## 3. User stories

1. As a TenantAdmin I want to post a Buy or Sell against any Account in my Tenant so that the corresponding Holding’s quantity and cost basis are updated automatically.
2. As an Adviser I want to post Transactions only against Accounts that belong to Customers assigned to me.
3. As a TenantAdmin or Adviser I want to list Transactions filtered by Account, date range and type, with pagination.
4. As a TenantAdmin or Adviser I want to view a single Transaction’s details.
5. As the system I must reject any new Transaction against a Closed Account.
6. As the system I must keep Holding.Quantity non-negative and apply average-cost adjustments correctly for Buy and Sell.
7. As the system I must prevent physical deletion of a Holding that still has historical Transactions.
8. As the system I must return 404 (never 403) when a caller requests a Transaction or Account outside their visibility.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only users with `Role = TenantAdmin` or `Role = Adviser` may access any Transaction endpoint. |
| R2 | All operations are strictly scoped to the caller’s visibility via the parent Account. TenantAdmin sees every Transaction under Accounts in the Tenant; Adviser sees only Transactions under Accounts whose Customer has `AdviserId` equal to the caller’s Domain User Id. Cross-scope or cross-tenant ids return 404. |
| R3 | The target Account must exist, be visible to the caller, and have `Status = Active`. Posting to a Closed Account returns 400. |
| R4 | `Amount.Amount` must be > 0. Direction is determined solely by `Type`; negative amounts are rejected. `Amount.Currency` must equal the parent Account’s `Currency`. |
| R5 | Buy / Sell: |
|    | - `HoldingId` is required and the Holding must belong to the same Account |
|    | - `Quantity` is required and must be > 0 |
|    | - For Sell, `Quantity` must not exceed the Holding’s current Quantity |
|    | - Inside the Account aggregate, Quantity and CostBasis are adjusted using the average-cost method |
| R6 | TransferIn / TransferOut / Dividend / Interest: `HoldingId` must be null (supplying one is 400). These types never modify any Holding. |
| R7 | `BookedOn` is required (`DateOnly`). Future dates are allowed. |
| R8 | `Note` is optional. |
| R9 | Transactions are append-only: no Update and no Delete endpoints or domain methods. |
| R10 | Create returns only the new Transaction id (`201 Created`). The client must re-query the Holding if it needs the updated position. |
| R11 | A Holding that is referenced by one or more Transactions cannot be physically deleted. `DeleteHolding` must check for existing references and return a clear business error. |
| R12 | List supports pagination plus optional filters `accountId`, `from`, `to`, `type`. No `holdingId` filter in MVP. |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| Transaction | Entity (inside Account aggregate) | Introduced in this slice. |
| TransactionType | Enum | Buy, Sell, TransferIn, TransferOut, Dividend, Interest. |
| Money | Value object | Used for Amount; currency must match Account. |

Fields (aligned with [domain-model.md](../domain-model.md)):

- `Id` : int (identity)
- `TenantId` : int (required, copied from Account)
- `AccountId` : int (required)
- `HoldingId` : int? (required for Buy/Sell; must be null for cash types)
- `BookedOn` : DateOnly (required)
- `Type` : TransactionType (required)
- `Amount` : Money (required, Amount > 0, currency = Account.Currency)
- `Quantity` : decimal? (required and > 0 for Buy/Sell; null for cash types)
- `Note` : string?
- Audit columns

Invariants:

- Append-only after creation.
- Cannot post to a Closed Account.
- Buy increases Holding.Quantity and adds the full Amount to Holding.CostBasis.
- Sell decreases Holding.Quantity and reduces Holding.CostBasis proportionally (average cost).
- Cash types leave Holdings untouched.
- A Holding that still has Transactions cannot be deleted (guard implemented in this slice).

Domain events (raise if cheap):

- `TransactionPosted` (raised by the Account aggregate)
- `HoldingChanged` (when a Buy or Sell adjusts a Holding)

Update [domain-model.md](../domain-model.md) only if further clarification is needed (Transaction is already described).

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| Transactions | add | See below |

Columns (aligned with [database-design.md](../database-design.md)):

- `Id` int identity PK
- `TenantId` int not null (**FK → Tenants**, ON DELETE RESTRICT)
- `AccountId` int not null (**FK → Accounts**, ON DELETE RESTRICT)
- `HoldingId` int null (**FK → Holdings**, ON DELETE RESTRICT)
- `BookedOn` date not null
- `Type` int not null (TransactionType enum)
- `Amount_Amount` decimal(18,4) not null
- `Amount_Currency` char(3) not null
- `Quantity` decimal(18,8) null
- `Note` nvarchar(500) null
- audit columns (`Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`)

Indexes:

- `(TenantId, AccountId, BookedOn)`
- `(AccountId, Type)`
- `(HoldingId)`

Notes:

- EnsureCreated is still used; no EF migration in this slice unless the project switches later.
- `HoldingId` FK is RESTRICT so the database also protects against accidental deletion of referenced Holdings.
- No cascade delete from Account → Transactions (history must be retained).

Update [database-design.md](../database-design.md) in the same change if any column precision or index needs tightening.

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | CreateTransaction | int (new Id) | Account Active & visible; Type-specific Holding/Quantity rules; Amount > 0; currency match; Sell quantity ≤ current Holding |
| Query | GetTransactions | PaginatedList\<TransactionVm\> | page, pageSize, accountId?, from?, to?, type? + automatic visibility filter |
| Query | GetTransactionById | TransactionVm (404 if missing or out of scope) | |

Also update the existing DeleteHolding command:

- Before deleting, check whether any Transaction references the Holding; if yes, reject with a clear business error.

Scaffold from `src/Application`:

```bash
dotnet new ca-usecase --name CreateTransaction --feature-name Transactions --usecase-type command --return-type int
dotnet new ca-usecase --name GetTransactions --feature-name Transactions --usecase-type query --return-type PaginatedList<TransactionVm>
dotnet new ca-usecase --name GetTransactionById --feature-name Transactions --usecase-type query --return-type TransactionVm
```

## 8. API

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/transactions` | TenantAdmin, Adviser | 200 + paginated list | 401, 403 | List with pagination and filters. |
| GET | `/transactions/{id}` | TenantAdmin, Adviser | 200 + TransactionVm | 401, 403, 404 | Single transaction (visibility-scoped). |
| POST | `/transactions` | TenantAdmin, Adviser | 201 + `{ "id": n }` | 400, 401, 403, 404 | Create transaction (returns id only). |

Query parameters for list:

- `page` (1-based, default 1)
- `pageSize` (default 20, max 100)
- `accountId` (optional int)
- `from` (optional date – BookedOn ≥)
- `to` (optional date – BookedOn ≤)
- `type` (optional: `Buy` \| `Sell` \| `TransferIn` \| `TransferOut` \| `Dividend` \| `Interest`)

Request body for POST (Buy example):

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

Request body for POST (Dividend example):

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

`holdingId` and `quantity` must be omitted (or null) for cash-only types.

Suggested `TransactionVm` shape:

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

Update [api-design.md](../api-design.md) in the same change.

## 9. UI

Adviser Portal (`src/AdviserPortal`) now implements Transactions for TenantAdmin and Adviser. Append-only: no edit or delete UI, and no dedicated detail page.

- **List** (`/transactions`): filters for account, date range (`from` / `to`), and type, plus pagination. Account names resolve from `GET /accounts` and link to account detail. Empty state: “No transactions” plus a create action.
- **Create** (`/transactions/new`): select Account first (Active accounts only). Type defaults to Buy. Buy / Sell require Holding + Quantity; cash types omit both. Amount is always positive; currency is taken from the selected account (not an input). `BookedOn` defaults to today; future dates are allowed. Note is optional (max 500). `?accountId=` prefills the account from the account detail overview.
- **Account detail overview**: paginated `GET /transactions?accountId=` table on `/accounts/:accountId`. “New transaction” is shown only while the account is Active.

## 10. Tests

| Project | Cases |
| --- | --- |
| Domain.UnitTests | Buy/Sell correctly adjust Quantity and average CostBasis; Sell quantity exceeding current Holding is rejected; Closed Account rejects post; cash types reject HoldingId; Holding with Transactions cannot be deleted |
| Application.UnitTests | Create rejects Closed / invisible Account, currency mismatch, negative Amount, wrong Holding ownership, Sell over-quantity; Adviser cannot target another Adviser’s Customer; non-allowed roles rejected by policy |
| Application.FunctionalTests | TenantAdmin happy path: create Buy → Holding updated → list (filter by accountId / date / type / page) → get; Adviser scoped correctly; cash types leave Holdings untouched; DeleteHolding blocked when Transactions exist; out-of-scope returns 404 |
| Web / Integration | 403 for SystemAdmin / anonymous; 404 for cross-tenant or other-Adviser Transaction; 400 on Closed Account or invalid Type rules |

## 11. Rollout

- [x] Feature spec accepted
- [x] Domain `Transaction` entity + `TransactionType` enum + invariants + average-cost logic on Account
- [x] EF configuration (FKs, indexes) + EnsureCreated / seed updates if needed
- [x] CreateTransaction / GetTransactions / GetTransactionById + validators
- [x] Harden DeleteHolding with reference check
- [x] Endpoints under `/transactions`
- [x] Tests
- [x] Parent docs updated (domain-model if needed, database-design, api-design, function-plan, features/README)

## 12. Open questions

- None remaining after 2026-08-24 decisions.
  - No `holdingId` filter on the list.
  - Amount is always positive; Type alone determines direction.
  - Future `BookedOn` dates are allowed.
  - Create returns only the new id (no Holding snapshot).
  - Existing decimal precisions (`Quantity` decimal(18,8), `Amount`/`CostBasis` decimal(18,4)) are sufficient for MVP.

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-26 | Adviser Portal: list + create pages, account-detail overview. No edit/delete UI. |
| 2026-08-24 | Implemented. Top-level `/transactions` create/list/get for TenantAdmin + Adviser. Append-only. Buy/Sell average-cost inside Account. Cash types leave Holdings untouched. Holding-delete guard landed. Amount always > 0. Future BookedOn allowed. Create returns id only. |
| 2026-08-24 | Created from discussion. Locked: append-only, top-level routes, Amount always > 0, future BookedOn allowed, create returns id only, no holdingId list filter, average-cost adjustment inside Account aggregate, Holding-delete guard delivered in this slice. |
