---
title: Function plan
status: draft
owner: ""
last_updated: 2026-08-16
related:
  - architecture.md
  - domain-model.md
  - database-design.md
  - api-design.md
---

# Function plan

Product scope and delivery order for MyWealth. Replace the example modules below once the first real slices are chosen.

## 1. Problem

<!-- One or two paragraphs: who this is for, what is painful today, what success looks like. -->

_MyWealth helps a single person (later: a household) see and manage personal wealth in one place — accounts, holdings, cash flow, and net worth over time._

## 2. Users and tenancy

| Actor | Can do | Cannot do |
| --- | --- | --- |
| Owner (authenticated user) | CRUD own data, view own reports | See another user's data |
| Administrator | _TBD — starter role already exists_ | _TBD_ |
| Guest / anonymous | Register, log in | Anything else |

**Tenancy decision:** _TBD — default assumption is one user owns their own data. Multi-user household sharing is out of scope until an ADR says otherwise._

**Identity today:** ASP.NET Identity with bearer tokens (`/users` Identity API). Seeded admin: `administrator@localhost`. Role constant: `Administrator`.

## 3. Goals and non-goals

### Goals (v1)

- [ ] _e.g. record accounts and current balances_
- [ ] _e.g. record transactions and categorize them_
- [ ] _e.g. compute net worth from accounts + holdings_
- [ ] _e.g. show a simple dashboard_

### Explicit non-goals (v1)

- [ ] Broker / bank live sync (Plaid, Open Banking, …)
- [ ] Tax filing
- [ ] Multi-currency FX engine beyond a stored currency code
- [ ] Mobile native apps
- [ ] Multi-tenant SaaS / orgs

## 4. Modules

Treat each module as a candidate bounded context. Promote a module to a [feature spec](features/README.md) when you start implementing it.

| Module | Purpose | v1? | Depends on | Feature specs |
| --- | --- | --- | --- | --- |
| Identity & profile | Register, login, logout, current user | Yes | — | |
| Accounts | Cash, bank, credit, brokerage, property, other | _TBD_ | Identity | |
| Holdings / positions | What is held inside an investment account | _TBD_ | Accounts | |
| Transactions | Money in/out, transfers, buys/sells | _TBD_ | Accounts | |
| Categories | Income / expense / transfer taxonomy | _TBD_ | — | |
| Net worth | Snapshot of assets − liabilities over time | _TBD_ | Accounts, Holdings | |
| Budget | Planned vs actual spend | Later | Transactions, Categories | |
| Goals | Target amounts and dates | Later | Net worth | |
| Import | CSV / statement import | Later | Transactions | |
| Reports | Cash flow, allocation, performance | Later | Several | |

Starter leftovers to remove when the first real module lands:

| Sample module | Location | Action |
| --- | --- | --- |
| Todo lists / items | Domain, Application, Web, tests | Replace |
| Weather forecasts | Application, Web | Remove |

## 5. Delivery phases

### Phase 0 — foundation (current)

- [x] Clean Architecture + Aspire AppHost
- [x] SQL Server (`MyWealthDb`) via Aspire
- [x] ASP.NET Identity + bearer tokens
- [x] CQRS pipeline (MediatR, FluentValidation, auth, logging)
- [ ] Replace Todo sample with the first real aggregate
- [ ] Switch DB init from `EnsureDeleted` / `EnsureCreated` to EF migrations (see [database design](database-design.md))

### Phase 1 — _name the first vertical slice_

<!-- One slice that a user can complete end-to-end. Example: "create an account and see it on the dashboard". -->

- [ ] Feature spec: `docs/features/<name>.md`
- [ ] Domain model update
- [ ] EF configuration + migration
- [ ] Commands / queries + validators
- [ ] Minimal API endpoints
- [ ] Tests (domain, application, functional)

### Phase 2 — _next slice_

- [ ]

### Phase 3 — _next slice_

- [ ]

## 6. Cross-cutting rules

Capture product rules here so feature specs can point at them instead of restating them.

| ID | Rule | Status |
| --- | --- | --- |
| FR-01 | Every domain entity is owned by a user (or a household, once that exists) | proposed |
| FR-02 | Money is stored as a precise decimal + ISO currency code; never `float` / `double` | proposed |
| FR-03 | Soft-delete vs hard-delete is decided per aggregate and written in the feature spec | proposed |
| FR-04 | Audit fields (`Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`) come from `BaseAuditableEntity` | accepted (already in code) |
| FR-05 | Write operations go through MediatR commands; reads go through queries | accepted (already in code) |

## 7. Open questions

- Single-user only, or household sharing in v1?
- Manual entry only, or import in v1?
- Which currencies, and is FX conversion in scope?
- Are investment lots / cost basis required for v1, or is a current-value holding enough?
- Will there be a separate SPA (`webfrontend` is reserved in `Shared/Services.cs`) or stay on Scalar + static files for a while?

## 8. Changelog

| Date | Change |
| --- | --- |
| 2026-08-16 | Template created from the current starter snapshot |
