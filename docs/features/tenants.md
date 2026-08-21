---
title: "Tenants"
status: draft
owner: ""
last_updated: 2026-08-22
related:
  - ../function-plan.md
  - ../domain-model.md
  - ../database-design.md
  - ../api-design.md
  - ../adr/0005-shared-database-tenantid-isolation.md
---

# Tenants

Foundation vertical slice for platform-level Tenant management. Only SystemAdmin can create, list, view and update Tenants. All subsequent business data (Advisers, Customers, Accounts, …) is rooted under a Tenant.

## 1. Summary

SystemAdmin can create new Tenants, list them with pagination / filtering / search, view a single Tenant, and update its name or enabled state. Creating a Tenant does **not** create any user account. Disabled Tenants remain visible to SystemAdmin on the same terms as enabled ones; the disable flag is intended to block future writes in later features.

## 2. Scope

**In**

- Create Tenant (Name, IsEnabled defaults to true)
- List Tenants with:
  - pagination
  - filter by IsEnabled
  - search by Id or Name (case-insensitive)
- Get Tenant by Id
- Update Tenant (Name and/or IsEnabled)
- Name uniqueness is **case-insensitive**
- Soft-disable / re-enable via IsEnabled (no physical delete)
- Only Role = SystemAdmin may call these endpoints
- Domain entity, EF configuration, commands/queries, endpoints, tests

**Out**

- Creating a TenantAdmin (or any user) at the same time as the Tenant
- Physical deletion of a Tenant
- Any business data that belongs to a Tenant (Advisers, Customers, Accounts, …)
- Tenant-level configuration, quotas, billing or branding
- SystemAdmin switching into a Tenant context (Option A is locked; MVP does not support it)
- UI (API only for MVP)

## 3. User stories

1. As a SystemAdmin I want to create a new Tenant so that a new wealth-management firm can be onboarded.
2. As a SystemAdmin I want to list all Tenants (with pagination, IsEnabled filter and Id/Name search) so that I can find and manage platform clients.
3. As a SystemAdmin I want to view a single Tenant’s details.
4. As a SystemAdmin I want to rename a Tenant or enable/disable it.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only users with `Role = SystemAdmin` may access any Tenant endpoint. |
| R2 | `Tenant.Name` is required and globally unique (case-insensitive). |
| R3 | Physical deletion is forbidden; only `IsEnabled` may be toggled. |
| R4 | Creating a Tenant does **not** create any Identity user or TenantAdmin. |
| R5 | A disabled Tenant is still visible to SystemAdmin on exactly the same terms as an enabled one (Option A already prevents SystemAdmin from seeing business data). |
| R6 | List endpoint must support pagination, `IsEnabled` filter, and search by Id or Name. The same list-shape pattern will be reused later for Advisers / Customers / Accounts. |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| Tenant | Aggregate root | Platform-level. No `TenantId` column of its own. |

Fields:

- `Id` : int (identity)
- `Name` : string (required, unique case-insensitive)
- `IsEnabled` : bool (default true)
- Audit columns (`Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`)

Invariants:

- Name must not be null or whitespace.
- Name uniqueness is enforced case-insensitively.

Domain events (optional for MVP):

- `TenantCreated`
- `TenantDisabled` / `TenantEnabled`

Update [domain-model.md](../domain-model.md) in the same change if any clarification is needed.

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| Tenants | add (already designed) | Unique index on `Name` (case-insensitive collation or computed lower-case column) |

Columns already specified in [database-design.md](../database-design.md):

- `Id` int identity PK
- `Name` nvarchar(200) not null
- `IsEnabled` bit not null default 1
- audit columns

Migration name: follow existing convention when the first real migration is introduced (currently EnsureCreated is still used).

Update [database-design.md](../database-design.md) only if the unique-index approach needs clarification.

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | CreateTenant | int (new Id) | Name required, unique (case-insensitive) |
| Command | UpdateTenant | — | Name required if supplied, unique; IsEnabled optional |
| Query | GetTenants | paginated list | page, pageSize, isEnabled?, search? |
| Query | GetTenantById | TenantVm or null | |

Scaffold from `src/Application`:

```bash
dotnet new ca-usecase --name CreateTenant --feature-name Tenants --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateTenant --feature-name Tenants --usecase-type command --return-type unit
dotnet new ca-usecase --name GetTenants --feature-name Tenants --usecase-type query --return-type PaginatedList<TenantVm>
dotnet new ca-usecase --name GetTenantById --feature-name Tenants --usecase-type query --return-type TenantVm
```

## 8. API

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/tenants` | SystemAdmin | 200 + paginated list | 401, 403 | List with pagination, `isEnabled` filter, `search` (Id or Name) |
| GET | `/tenants/{id}` | SystemAdmin | 200 + TenantVm | 401, 403, 404 | Single Tenant |
| POST | `/tenants` | SystemAdmin | 201 + id | 400, 401, 403 | Create |
| PUT | `/tenants/{id}` | SystemAdmin | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled |

Query parameters for list (to be reused by later list endpoints):

- `page` (1-based, default 1)
- `pageSize` (default 20, max 100)
- `isEnabled` (optional bool)
- `search` (optional string – matches Id exactly or Name contains, case-insensitive)

Update [api-design.md](../api-design.md) in the same change.

## 9. UI

None — API only (SystemAdmin management UI is out of MVP Adviser Portal scope).

## 10. Tests

| Project | Cases |
| --- | --- |
| Application.UnitTests | Create rejects duplicate Name (case-insensitive); Update cannot set empty Name; non-SystemAdmin policy |
| Application.FunctionalTests | Full SystemAdmin happy path: create → list (filter/search/page) → get → update → disable |
| Web / Integration | 403 for TenantAdmin / Adviser / anonymous; 404 for unknown id |

## 11. Rollout

- [ ] Feature spec accepted
- [ ] Domain `Tenant` entity
- [ ] EF configuration + unique index (case-insensitive)
- [ ] Commands / queries / validators
- [ ] Endpoints under `/tenants`
- [ ] Tests
- [ ] Parent docs updated (model, DB, API, function plan)

## 12. Open questions

- None remaining after 2026-08-22 decisions.

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-22 | Created. Decisions locked: no user creation on Tenant create; Name unique case-insensitive; SystemAdmin visibility unchanged by IsEnabled; list supports pagination + IsEnabled filter + Id/Name search (pattern to be reused). |
