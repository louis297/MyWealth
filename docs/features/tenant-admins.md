---
title: "Tenant Admins"
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
  - ../features/advisers.md
  - ../adr/0005-shared-database-tenantid-isolation.md
  - ../adr/0006-email-password-jwt-authentication.md
  - ../adr/0007-baseentity-primary-key-int.md
---

# Tenant Admins

Platform-level vertical slice for SystemAdmin to manage TenantAdmin accounts. Creating a TenantAdmin also creates the corresponding login-capable Identity user. Domain `User` was already introduced by the Advisers slice; this feature only creates and manages rows with `Role = TenantAdmin`.

## 1. Summary

SystemAdmin can onboard a TenantAdmin for any existing **enabled** Tenant (with an initial password), list TenantAdmins with the same pagination / filter / search shape used elsewhere (plus optional TenantId filter), view a single TenantAdmin, update name or enabled state, and soft-disable a TenantAdmin. This closes the gap left by the Tenants feature (which deliberately does not create users) so that a newly created Tenant can receive a working administrator without relying solely on seed data.

## 2. Scope

**In**

- Create TenantAdmin (TenantId, Name, Email, Password; IsEnabled defaults to true)
  - Target Tenant must exist **and** `IsEnabled = true`
  - Creates Domain `User` (Role = TenantAdmin, TenantId = supplied, AdviserId = null)
  - Creates corresponding `ApplicationUser` (Role = TenantAdmin, same TenantId, DisplayName, Email, Password) so the TenantAdmin can log in
- List TenantAdmins with:
  - pagination
  - filter by IsEnabled
  - optional filter by TenantId
  - search by Id, Name or Email (case-insensitive)
- Get TenantAdmin by Id
- Update TenantAdmin (Name and/or IsEnabled)
- Soft-disable via `DELETE /tenant-admins/{id}` (sets IsEnabled = false on both Domain User and ApplicationUser)
- Re-enable is allowed via Update (IsEnabled = true) with no extra checks
- Only Role = SystemAdmin may call these endpoints
- Email is **globally unique**
- Domain invariants already present on `User` + commands/queries + endpoints + tests

**Out**

- Creating or managing SystemAdmin accounts (seed / bootstrap only)
- Creating or managing Advisers / Customers (owned by their respective features)
- Physical deletion of any User
- Password-reset / forgot-password flows
- SystemAdmin switching into a Tenant context (Option A remains locked)
- Nested routes under `/tenants/{id}/admins` (flat `/tenant-admins` is used)
- UI pages (API only for this slice)
- Any guard that prevents disabling the “last” TenantAdmin of a Tenant (explicitly allowed; SystemAdmin can always re-create)

## 3. User stories

1. As a SystemAdmin I want to create a TenantAdmin for an existing enabled Tenant (with initial login credentials) so that the firm can start managing its Advisers and Customers.
2. As a SystemAdmin I want to list all TenantAdmins (with pagination, IsEnabled / TenantId filters and Id/Name/Email search) so that I can find and manage platform operators.
3. As a SystemAdmin I want to view a single TenantAdmin’s details (including which Tenant they belong to).
4. As a SystemAdmin I want to rename a TenantAdmin or enable/disable the account (including the last one).
5. As the system I must reject creation when the target Tenant does not exist or is disabled.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only users with `Role = SystemAdmin` may access any TenantAdmin endpoint. |
| R2 | `TenantId` on create must reference an existing Tenant with `IsEnabled = true`. Missing or disabled Tenant → 400. |
| R3 | `Email` is required and **globally unique** (Demo simplification; matches ASP.NET Identity default and Advisers / Customers decisions). |
| R4 | Creating a TenantAdmin must also create a login-capable `ApplicationUser` with the supplied Password (Identity strength rules apply). The two writes occur in one transaction. |
| R5 | Physical deletion is forbidden. Soft-disable sets `IsEnabled = false` on both Domain `User` and `ApplicationUser`. |
| R6 | Re-enable (`IsEnabled = true`) has no extra preconditions. |
| R7 | List endpoint reuses the pagination / filter / search shape already established by Tenants / Advisers / Customers, plus an optional `tenantId` query parameter. |
| R8 | Domain `User` rows for TenantAdmins always have `Role = TenantAdmin`, non-null `TenantId`, and null `AdviserId`. |
| R9 | SystemAdmin itself is never managed by these endpoints (TenantId remains null). |
| R10 | Disabled TenantAdmins remain visible to SystemAdmin on the same terms as enabled ones. |
| R11 | Disabling the last remaining enabled TenantAdmin of a Tenant is **allowed**. SystemAdmin can always create a new one later. |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| User | Aggregate root | Already introduced by Advisers. This feature only creates/manages Role = TenantAdmin. |

Fields (relevant to this slice):

- `Id` : int (identity)
- `TenantId` : int (required for TenantAdmin)
- `Name` : string (required)
- `Email` : string (required, globally unique)
- `IsEnabled` : bool (default true)
- `Role` : UserRole (TenantAdmin)
- `AdviserId` : int? (always null for TenantAdmin)
- `IdentityUserId` : string? (link to AspNetUsers.Id)
- Audit columns

Invariants (already on `User`; enforced again at application layer):

- Role = TenantAdmin ⇒ TenantId is required and AdviserId is null.
- Email must not be null or whitespace and must be globally unique.
- Name must not be null or whitespace.

Domain events (optional for MVP, raise if cheap):

- `UserCreated`
- `UserDisabled` / `UserEnabled`

No structural change required to [domain-model.md](../domain-model.md). TenantAdmin creation is now an API-supported path (no longer seed-only).

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| Users | no structural change | already present from Advisers |

Columns (already defined in [database-design.md](../database-design.md)):

- `Id` int identity PK
- `TenantId` int null (**FK → Tenants**, ON DELETE RESTRICT; required for TenantAdmin)
- `Name` nvarchar(200) not null
- `Email` nvarchar(256) not null (unique)
- `IsEnabled` bit not null default 1
- `Role` int not null
- `AdviserId` int null
- `IdentityUserId` nvarchar(450) null (no hard FK)
- audit columns

No new indexes or migrations are required for this slice (EnsureCreated still in use).

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | CreateTenantAdmin | int (new Id) | TenantId, Name, Email, Password required; Email unique; Password meets Identity rules; Tenant must exist **and** be enabled |
| Command | UpdateTenantAdmin | — | Name required if supplied; IsEnabled optional |
| Command | DisableTenantAdmin | — | No “last admin” check |
| Query | GetTenantAdmins | paginated list | page, pageSize, isEnabled?, tenantId?, search? |
| Query | GetTenantAdminById | TenantAdminVm (404 if missing) | |

Scaffold from `src/Application`:

```bash
dotnet new ca-usecase --name CreateTenantAdmin --feature-name TenantAdmins --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateTenantAdmin --feature-name TenantAdmins --usecase-type command --return-type unit
dotnet new ca-usecase --name DisableTenantAdmin --feature-name TenantAdmins --usecase-type command --return-type unit
dotnet new ca-usecase --name GetTenantAdmins --feature-name TenantAdmins --usecase-type query --return-type PaginatedList<TenantAdminVm>
dotnet new ca-usecase --name GetTenantAdminById --feature-name TenantAdmins --usecase-type query --return-type TenantAdminVm
```

## 8. API

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/tenant-admins` | SystemAdmin | 200 + paginated list | 400, 401, 403 | List with pagination, `isEnabled` / `tenantId` filters, `search` (Id, Name or Email) |
| GET | `/tenant-admins/{id}` | SystemAdmin | 200 + TenantAdminVm | 401, 403, 404 | Single TenantAdmin |
| POST | `/tenant-admins` | SystemAdmin | 201 + `{ "id": n }` | 400, 401, 403 | Create Domain User + Identity login. Email globally unique. Target Tenant must be enabled. |
| PUT | `/tenant-admins/{id}` | SystemAdmin | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled. Route `{id}` must match body `id`. |
| DELETE | `/tenant-admins/{id}` | SystemAdmin | 204 | 400, 401, 403, 404 | Soft-disable (`IsEnabled = false` on Domain User and Identity). Last admin is allowed. |

Query parameters for list (extends the shape used by Tenants / Advisers / Customers):

- `page` (1-based, default 1, min 1)
- `pageSize` (default 20, min 1, max 100)
- `isEnabled` (optional bool)
- `tenantId` (optional int — exact match)
- `search` (optional string — matches Id exactly or Name/Email contains, case-insensitive)

POST body:

```json
{
  "tenantId": 1,
  "name": "Alice Chen",
  "email": "alice.chen@acme.com",
  "password": "P@ssw0rd!"
}
```

`TenantAdminVm`:

```json
{
  "id": 5,
  "tenantId": 1,
  "tenantName": "Acme Wealth",
  "name": "Alice Chen",
  "email": "alice.chen@acme.com",
  "isEnabled": true
}
```

PUT is partial: omitted `name` / `isEnabled` stay unchanged. Supplying neither is 400. Re-enable is `PUT` with `isEnabled: true` and has no extra preconditions.

Update [api-design.md](../api-design.md) in the same change (catalog already updated).

## 9. UI

None — API only (SystemAdmin management UI is out of MVP Adviser Portal scope).

## 10. Tests

| Project | Cases |
| --- | --- |
| Application.UnitTests | Create rejects duplicate Email; Create rejects unknown TenantId; Create rejects disabled Tenant; non-SystemAdmin policy |
| Application.FunctionalTests | Full SystemAdmin happy path: create → list (filter/search/page/tenantId) → get → update → disable (including last admin) → re-enable; created TenantAdmin can log in with the supplied password |
| Web / Integration | 403 for TenantAdmin / Adviser / anonymous; 404 for unknown id; 400 on missing/disabled TenantId or weak password |

## 11. Rollout

- [x] Feature spec accepted
- [ ] Commands / queries / validators (including transactional Create)
- [ ] Endpoints under `/tenant-admins`
- [ ] Tests
- [x] Parent docs updated (api-design catalog, function-plan, features/README; Advisers Out section already points here)
- [ ] Seed data may keep a bootstrap TenantAdmin for local convenience, but is no longer the only path

## 12. Open questions

- None remaining.

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-24 | Accepted. Locked: create rejects disabled Tenant; disabling the last TenantAdmin is explicitly allowed; flat `/tenant-admins`; SystemAdmin only; caller-supplied password; global unique Email; soft-disable via DELETE; optional `tenantId` filter. |
| 2026-08-24 | Created as draft. |
