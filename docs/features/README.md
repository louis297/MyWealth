# Feature specs

One file per vertical slice. A slice is implementable on its own: domain + persistence + command/query + endpoint + tests.

## How to add one

1. Copy [`../_templates/feature.md`](../_templates/feature.md) to `docs/features/<kebab-name>.md`.
2. Fill sections 1–10 before writing production code (open questions can stay open).
3. Link it from the module table in [function-plan.md](../function-plan.md).
4. When the slice ships, set `status: accepted` and update model / DB / API docs.

## Index

| Feature | Status | Phase | Notes |
| --- | --- | --- | --- |
| | | | _none yet — Todo sample is not a MyWealth feature_ |

Suggested first slices (edit freely):

| Candidate | Why first |
| --- | --- |
| `accounts` | Everything else hangs off an account |
| `categories` | Small, unblocks transactions |
| `transactions` | Core cash-flow loop |
| `holdings` | Only if investing is in v1 |
| `net-worth` | Read model on top of accounts / holdings |
