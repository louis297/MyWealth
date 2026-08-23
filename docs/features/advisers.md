---
title: "Advisers"
status: accepted
owner: ""
last_updated: 2026-08-23
related:
  - ../function-plan.md
  - ../domain-model.md
  - ../database-design.md
  - ../api-design.md
  - ../features/identity-auth.md
  - ../features/tenants.md
  - ../adr/0005-shared-database-tenantid-isolation.md
  - ../adr/0006-email-password-jwt-authentication.md
  - ../adr/0007-baseentity-primary-key-int.md
---

# Advisers

Tenant-scoped vertical slice for managing Advisers. Only TenantAdmin can create, list, view, update and soft-disable Advisers belonging to their own Tenant. Creating an Adviser also creates the corresponding login-capable Identity user. Soft-disable requires that no Customers remain assigned to the Adviser.

## 1. Summary

TenantAdmin can onboard new Advisers (with an initial password), list them with the same pagination / filter / search shape already used by Tenants, view a single Adviser, update name or enabled state, and soft-disable an Adviser. Disable is refused while any Customer still references the Adviser. The Domain `User` aggregate is introduced in this slice (Role = Adviser). Email is globally unique (Demo simplification aligned with ASP.NET Identity defaults).

## 2. Scope

**In**

- Introduce Domain aggregate root `User` (full foundation, including `AdviserId` column and indexes even though Customer feature is not yet implemented)
- Create Adviser (Name, Email, Password; IsEnabled defaults to true)
  - Creates Domain `User` (Role = Adviser, TenantId = caller's TenantId, AdviserId = null)
  - Creates corresponding `ApplicationUser` (Role = Adviser, same TenantId, DisplayName, Email, Password) so the Adviser can log in
- List Advisers inside the current Tenant with:
  - pagination
  - filter by IsEnabled
  - search by Id, Name or Email (case-insensitive)
- Get Adviser by Id (scoped to current Tenant)
- Update Adviser (Name and/or IsEnabled)
- Soft-disable via `DELETE /advisers/{id}` (sets IsEnabled = false)
  - Must fail if any Customer still has this Adviser as `AdviserId`
- Re-enable is allowed via Update (IsEnabled = true) with no extra checks
- Only Role = TenantAdmin may call these endpoints
- Email is **globally unique**
- Domain entity + EF configuration + commands/queries + endpoints + tests

**Out**

- Creating or managing TenantAdmin accounts (seed data only for MVP)
- Customer CRUD (next feature)
- Physical deletion of any User
- Adviser self-service profile changes (already covered by `/users/me`)
- SystemAdmin managing any business users (Option A remains locked)
- Password-reset / forgot-password flows
- UI pages (API only for this slice)
- Changing the login model to require Tenant context (explicitly deferred; Email stays globally unique as a Demo simplification)

## 3. User stories

1. As a TenantAdmin I want to create a new Adviser (with initial login credentials) so that I can assign Customers to a specific adviser.
2. As a TenantAdmin I want to list all Advisers in my Tenant (with pagination, IsEnabled filter and Id/Name/Email search) so that I can find and manage them.
3. As a TenantAdmin I want to view a single Adviser's details.
4. As a TenantAdmin I want to rename an Adviser or enable/disable the account.
5. As the system I must reject soft-disable of an Adviser who still has Customers assigned, forcing reassignment first.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only users with `Role = TenantAdmin` may access any Adviser endpoint. |
| R2 | All operations are strictly scoped to the caller's `TenantId` (from JWT). Cross-tenant ids return 404. |
| R3 | `Email` is required and **globally unique** (Demo simplification; matches ASP.NET Identity default). |
| R4 | Creating an Adviser must also create a login-capable `ApplicationUser` with the supplied Password (Identity strength rules apply). |
| R5 | Physical deletion is forbidden. Soft-disable sets `IsEnabled = false` on both Domain `User` and `ApplicationUser`. |
| R6 | Soft-disable (`DELETE`) must fail with a business error (400) while any Customer still references this User via `AdviserId`. |
| R7 | Re-enable (`IsEnabled = true`) has no extra preconditions. |
| R8 | List endpoint reuses the pagination / filter / search shape already established by Tenants. |
| R9 | Domain `User` rows for Advisers always have `Role = Adviser`, non-null `TenantId`, and null `AdviserId`. |
| R10 | `TenantAdmin` rows also live in the same `Users` table (Role = TenantAdmin) but are not managed by this feature's endpoints. |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| User | Aggregate root | Introduced in this slice. Holds all four roles; this feature only creates/manages Role = Adviser. |

Fields (relevant to this slice):

- `Id` : int (identity)
- `TenantId` : int (required for Adviser)
- `Name` : string (required)
- `Email` : string (required, globally unique)
- `IsEnabled` : bool (default true)
- `Role` : UserRole (Adviser)
- `AdviserId` : int? (always null for Adviser)
- `IdentityUserId` : string? (link to AspNetUsers.Id)
- Audit columns

Invariants:

- Role = Adviser ⇒ TenantId is required and AdviserId is null.
- Email must not be null or whitespace and must be globally unique.
- A User cannot be soft-disabled while any Customer still references it as Adviser.
- Name must not be null or whitespace.

Domain events (optional for MVP, raise if cheap):

- `UserCreated`
- `UserDisabled` / `UserEnabled`

Update [domain-model.md](../domain-model.md) in the same change when the entity is implemented.

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| Users | add (already designed) | See below |

Columns (aligned with [database-design.md](../database-design.md)):

- `Id` int identity PK
- `TenantId` int null (null only for SystemAdmin; **FK → Tenants**, ON DELETE RESTRICT)
- `Name` nvarchar(200) not null
- `Email` nvarchar(256) not null
- `IsEnabled` bit not null default 1
- `Role` int not null
- `AdviserId` int null (**FK → Users**, ON DELETE RESTRICT)
- `IdentityUserId` nvarchar(450) null (**no FK** to AspNetUsers — intentional loose coupling)
- audit columns

Indexes:

- Unique on `Email` (global)
- `(TenantId, Role)`
- `AdviserId`
- Unique filtered index on `IdentityUserId` where not null (optional but recommended)

Notes:

- `ApplicationUser.TenantId` continues to have **no** FK to Tenants (already documented decision).
- Real referential integrity for tenant membership lives on Domain `Users.TenantId`.
- EnsureCreated is still used; no EF migration in this slice unless the project switches later.

Update [database-design.md](../database-design.md) only if any clarification of the FK / uniqueness decisions is needed.

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | CreateAdviser | int (new Id) | Name, Email, Password required; Email unique; Password meets Identity rules |
| Command | UpdateAdviser | — | Name required if supplied; IsEnabled optional |
| Command | DisableAdviser | — | Fails if any Customer still references this AdviserId |
| Query | GetAdvisers | paginated list | page, pageSize, isEnabled?, search? |
| Query | GetAdviserById | AdviserVm (404 if missing or wrong tenant) | |

Scaffold from `src/Application`:

```bash
dotnet new ca-usecase --name CreateAdviser --feature-name Advisers --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateAdviser --feature-name Advisers --usecase-type command --return-type unit
dotnet new ca-usecase --name DisableAdviser --feature-name Advisers --usecase-type command --return-type unit
dotnet new ca-usecase --name GetAdvisers --feature-name Advisers --usecase-type query --return-type PaginatedList<AdviserVm>
dotnet new ca-usecase --name GetAdviserById --feature-name Advisers --usecase-type query --return-type AdviserVm
```

CreateAdviser must run inside a transaction that covers both Domain `User` insert and `UserManager.CreateAsync`.

## 8. API

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/advisers` | TenantAdmin | 200 + paginated list | 401, 403 | List with pagination, `isEnabled` filter, `search` (Id / Name / Email) |
| GET | `/advisers/{id}` | TenantAdmin | 200 + AdviserVm | 401, 403, 404 | Single Adviser (tenant-scoped) |
| POST | `/advisers` | TenantAdmin | 201 + id | 400, 401, 403 | Create Adviser + Identity user |
| PUT | `/advisers/{id}` | TenantAdmin | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled |
| DELETE | `/advisers/{id}` | TenantAdmin | 204 | 400, 401, 403, 404 | Soft-disable. Fails with 400 if Customers still assigned. |

Query parameters for list (same shape as Tenants):

- `page` (1-based, default 1)
- `pageSize` (default 20, max 100)
- `isEnabled` (optional bool)
- `search` (optional string – matches Id exactly or Name/Email contains, case-insensitive)

Request body for POST:

```json
{
  "name": "Jane Smith",
  "email": "jane.smith@acme.com",
  "password": "P@ssw0rd!"
}
```

Update [api-design.md](../api-design.md) in the same change if any detail needs tightening.

## 9. UI

None — API only for this slice. Adviser list / create / edit pages in the Adviser Portal can be added later once the API is stable.

## 10. Tests

| Project | Cases |
| --- | --- |
| Domain.UnitTests | User invariants (Role/TenantId/AdviserId rules); cannot disable while referenced |
| Application.UnitTests | Create rejects duplicate Email; Disable fails when Customers exist; non-TenantAdmin policy |
| Application.FunctionalTests | Full TenantAdmin happy path: create → list (filter/search/page) → get → update → disable → re-enable; created Adviser can log in with the supplied password |
| Web / Integration | 403 for Adviser / SystemAdmin / anonymous; 404 for cross-tenant id; 400 when disabling an Adviser that still has Customers |

## 11. Rollout

- [x] Feature spec accepted
- [x] Domain `User` entity + invariants + optional events
- [x] EF configuration (FKs, unique Email, indexes) + EnsureCreated / seed updates
- [x] Commands / queries / validators (including transactional Create)
- [x] Endpoints under `/advisers`
- [x] Tests
- [x] Parent docs updated (domain-model, database-design, api-design, function-plan, features/README)

## 12. Open questions

- None remaining after 2026-08-23 decisions.
  - Password supplied by caller
  - Email globally unique (Demo simplification)
  - Soft-disable via DELETE
  - Full Domain User table (including AdviserId) introduced now
  - TenantAdmin lives in the same Users table
  - `Users.TenantId` → Tenants FK yes; `Users.AdviserId` → Users FK yes; `Users.IdentityUserId` → AspNetUsers **no hard FK**

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-23 | Implemented. Domain `User` introduced (all four roles). Create Adviser also creates Identity user in a transaction. Email globally unique (CI collation). Soft-disable via DELETE with Customer-reassignment guard. Seed links Domain Users to Identity; Customer seed is Domain-only (no login). |
| 2026-08-23 | Created from discussion. Locked: global unique Email (Demo), caller-supplied password, DELETE soft-disable with Customer-reassignment guard, full User foundation, FK decisions for TenantId/AdviserId/IdentityUserId. |
