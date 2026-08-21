# Feature specs

One file per vertical slice. A slice is implementable on its own: **domain + persistence + command/query + endpoint + tests**.

Frontend (Adviser Portal) is **optional** in the Feature Spec. Section 9 (UI) may be filled when the slice has meaningful UI impact, or marked `"None — API only"`.

## How to add one

1. Copy [`../_templates/feature.md`](../_templates/feature.md) to `docs/features/<kebab-name>.md`.
2. Fill sections 1–10 before writing production code (open questions can stay open).
3. Link it from the module table in [function-plan.md](../function-plan.md).
4. When the slice ships, set `status: accepted` and update model / DB / API docs in the same change.

## Index

| Feature | Status | Phase | Notes |
| --- | --- | --- | --- |
| [identity-auth](identity-auth.md) | accepted | Foundation | Login / Logout / Profile / JWT / Role claims. Customer login → 403. `/users/me` introduced. API only. |
| [tenants](tenants.md) | draft | Phase 1 | SystemAdmin only. CRUD tenants (Option A). |
| [advisers](advisers.md) | draft | Phase 1 | TenantAdmin manages Advisers. Soft-disable requires reassigning Customers. |
| [customers](customers.md) | draft | Phase 2 | Create / list / update / soft-disable. Must bind to an Adviser. |
| [accounts](accounts.md) | draft | Phase 2 | Account lifecycle. Close is permanent (`Status = Closed`). |
| [holdings](holdings.md) | draft | Phase 3 | Nested under `/accounts/{accountId}/holdings`. |
| [transactions](transactions.md) | draft | Phase 3 | Create + auto-adjust Holding quantity & cost basis (core write path). |
| [dashboard](dashboard.md) | draft | Phase 4 | Net Worth + asset allocation (read models). |
| [audit-log](audit-log.md) | draft | Phase 4 | View audit entries. Can be deferred. |

## Implementation order (dependency)

```
identity-auth
    ├── tenants
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
