# Feature specs

One file per vertical slice. A slice is implementable on its own: **domain + persistence + command/query + endpoint + tests**.

Frontend (Adviser Portal) is **optional** in the Feature Spec. Section 9 (UI) may be filled when the slice has meaningful UI impact, or marked `"None — API only"`.

## How to add one

1. Copy [`../_templates/feature.md`](../_templates/feature.md) to `docs/features/<kebab-name>.md`.
2. Fill sections 1–10 before writing production code (open questions can stay open).
3. Link it from the module table in [function-plan.md](../function-plan.md).
4. When the slice ships, set `status: accepted` and update model / DB / API docs in the same change.

## Model vs Slice discipline

- `domain-model.md` and `database-design.md` describe the **target** model. They may (and should) be more complete than the features that have already shipped. Prefer updating them proactively when a clearer design emerges.
- A Feature Spec remains a **strict vertical slice**: only the commands, queries, endpoints, invariants and tests that ship in *this* delivery belong in its “In” scope.
- When implementing a feature you may add Entity + EF Configuration for tables/relationships that are already defined in the model docs *and* required by the current slice (foreign keys, etc.). Do **not** implement handlers, validators or endpoints that belong to later features.

## Agentic coding conventions

When generating an implementation plan for a feature:

- Break the work into clear, sequential steps.
- Explicitly mark each step that should produce a git commit (e.g. “Step 3 → commit: add Tenant entity + EF configuration”).
- Prefer small, focused commits that keep the repository in a buildable state after each commit.
- Do not batch unrelated changes into a single commit.

## Index

| Feature | Status | Phase | Notes |
| --- | --- | --- | --- |
| [identity-auth](identity-auth.md) | accepted | Foundation | Login / Logout / Profile / JWT. Role = `UserRole` enum on ApplicationUser. Customer → 403. `/users/me` introduced. |
| [tenants](tenants.md) | accepted | Phase 1 | SystemAdmin only. CRUD + paginated list (filter/search). Name unique case-insensitive. No user creation on create. |
| [tenant-admins](tenant-admins.md) | accepted | Phase 1 | SystemAdmin manages TenantAdmins. Create also creates Identity user. Soft-disable via DELETE. Create rejects disabled Tenant. Last-admin disable allowed. Email globally unique (Demo). |
| [advisers](advisers.md) | accepted | Phase 1 | TenantAdmin manages Advisers. Introduces Domain User. Create also creates Identity user. Soft-disable (DELETE) requires no remaining Customers. Email globally unique (Demo). |
| [customers](customers.md) | accepted | Phase 2 | TenantAdmin + Adviser. Must bind to Adviser. No Identity user. Adviser can only manage/assign own Customers. Soft-disable (DELETE). Email globally unique. Account-existence guard deferred to Accounts feature. |
| [accounts](accounts.md) | draft | Phase 2 | TenantAdmin + Adviser. Create / list / get / update Name+Type / permanent close. No forced clear of Holdings/Transactions. Currency immutable. Optional customerId filter. |
| [holdings](holdings.md) | draft | Phase 3 | Nested under `/accounts/{accountId}/holdings`. Create/list/get/update/delete. Quantity ≥ 0 (incl. zero). CostBasis.Currency immutable & must match Account. No list search/pagination. Physical DELETE allowed; “no delete if historical Tx” guard is delivered by the Transactions feature. API only. |
| [transactions](transactions.md) | draft | Phase 3 | Top-level `/transactions`. Append-only create + list/get. Buy/Sell auto-adjust Holding (average cost) inside Account aggregate. Cash types leave Holdings untouched. Amount always > 0. Holding-delete guard (no delete if historical Tx) delivered here. API only. |
| [dashboard](dashboard.md) | draft | Phase 4 | Net Worth + asset allocation (read models). |
| [audit-log](audit-log.md) | draft | Phase 4 | View audit entries. Can be deferred. |

## Implementation order (dependency)

```
identity-auth
    ├── tenants
    │       └── tenant-admins
    └── advisers
            └── customers
                    └── accounts
                            ├── holdings
                            │       └── transactions
                            └── dashboard
audit-log (independent, can be last)
```

## Out of scope for MVP Feature Specs

- Categories (confirmed out of MVP)
- Customer login / Customer Portal
- Historical Net Worth snapshots
- Update or delete of Transactions
- Physical deletion of core entities
- Re-opening a Closed Account
