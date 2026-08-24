---
title: "Customers"
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
  - ../features/accounts.md
  - ../adr/0005-shared-database-tenantid-isolation.md
  - ../adr/0006-email-password-jwt-authentication.md
  - ../adr/0007-baseentity-primary-key-int.md
---

# Customers

Tenant-scoped vertical slice for managing Customers. TenantAdmin can manage every Customer inside their Tenant; Advisers can only manage Customers assigned to themselves. Creating a Customer requires a valid AdviserId and does **not** create an Identity login. Soft-disable is recoverable. Domain `User` was already introduced by the Advisers slice; this feature only creates and manages rows with `Role = Customer`.

## 1. Summary

TenantAdmin and Adviser can onboard Customers (must bind to an Adviser), list them with the same pagination / filter / search shape used by Tenants and Advisers, view a single Customer, update name / enabled state / Adviser assignment, and soft-disable a Customer. Advisers are restricted to their own Customers (create, reassign and visibility). Email remains globally unique (Demo simplification). Customers cannot authenticate in MVP.

## 2. Scope

**In**

- Create Customer (Name, Email, AdviserId required; IsEnabled defaults to true)
  - Creates Domain `User` only (Role = Customer, TenantId = caller's TenantId, AdviserId required)
  - Does **not** create an `ApplicationUser`; `IdentityUserId` stays null
- List Customers with:
  - pagination
  - filter by IsEnabled
  - search by Id, Name or Email (case-insensitive)
  - automatic visibility scoping (TenantAdmin = whole tenant; Adviser = own Customers only)
- Get Customer by Id (scoped; out-of-scope returns 404)
- Update Customer (Name and/or IsEnabled and/or reassign AdviserId)
- Soft-disable via `DELETE /customers/{id}` (sets IsEnabled = false)
- Re-enable is allowed via Update (IsEnabled = true) with no extra checks
- Roles allowed: TenantAdmin and Adviser
- Email is **globally unique**
- Domain invariants already present on `User` + commands/queries + endpoints + tests

**Out**

- Customer login / Customer Portal (explicitly blocked at Identity layer)
- Creating or managing TenantAdmin or Adviser accounts
- Physical deletion of any User
- Account / Holding / Transaction operations (next feature)
- Soft-disable guard that checks for existing Accounts (delivered by the Accounts feature: refuse disable while any **Active** account remains; Closed accounts do not block)
- UI pages (API only for this slice)
- Password or Identity-user creation for Customers
- SystemAdmin managing any business users (Option A remains locked)

## 3. User stories

1. As a TenantAdmin I want to create a Customer bound to any Adviser in my Tenant so that the firm can start managing that client's wealth.
2. As an Adviser I want to create a Customer that is automatically assigned to me so that I can manage my own book.
3. As a TenantAdmin or Adviser I want to list Customers (with pagination, IsEnabled filter and Id/Name/Email search) so that I can find them quickly. Advisers only see their own.
4. As a TenantAdmin or Adviser I want to view a single Customer's details (including the assigned Adviser).
5. As a TenantAdmin or Adviser I want to rename a Customer, enable/disable the record, or reassign the Adviser.
6. As the system I must return 404 (never 403) when an Adviser requests a Customer that does not belong to them.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only users with `Role = TenantAdmin` or `Role = Adviser` may access any Customer endpoint. |
| R2 | All operations are strictly scoped to the caller's visibility. TenantAdmin sees every Customer in the Tenant; Adviser sees only rows where `AdviserId` equals the caller's Domain User Id. Cross-scope or cross-tenant ids return 404. |
| R3 | `Email` is required and **globally unique** (Demo simplification; matches ASP.NET Identity default and Advisers decision). |
| R4 | Creating a Customer creates only a Domain `User`. No `ApplicationUser` is created and no password is accepted. |
| R5 | Physical deletion is forbidden. Soft-disable sets `IsEnabled = false` on the Domain `User`. |
| R6 | On create and on reassignment, `AdviserId` must reference an existing User with `Role = Adviser`, same `TenantId`, and `IsEnabled = true`. |
| R7 | An Adviser caller may only set `AdviserId` to their own Domain User Id (both on create and on update). TenantAdmin has no such restriction. |
| R8 | Re-enable (`IsEnabled = true`) has no extra preconditions. |
| R9 | List endpoint reuses the pagination / filter / search shape already established by Tenants and Advisers. |
| R10 | Domain `User` rows for Customers always have `Role = Customer`, non-null `TenantId`, and non-null `AdviserId`. |
| R11 | Soft-disable of a Customer is refused while any **Active** Account remains (delivered by the Accounts feature). Closed accounts do not block disable. |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| User | Aggregate root | Already introduced by Advisers. This feature only creates/manages `Role = Customer`. |

Fields (relevant to this slice):

- `Id` : int (identity)
- `TenantId` : int (required for Customer)
- `Name` : string (required)
- `Email` : string (required, globally unique)
- `IsEnabled` : bool (default true)
- `Role` : UserRole (Customer)
- `AdviserId` : int (required for Customer)
- `IdentityUserId` : string? (always null for Customer in MVP)
- Audit columns

Invariants (already present on `User`, reinforced here):

- Role = Customer ⇒ TenantId is required and AdviserId is required.
- The AdviserId target must have Role = Adviser, the same TenantId, and be enabled.
- Email must not be null or whitespace and must be globally unique.
- Name must not be null or whitespace.
- An Adviser cannot be the target of a reassignment if the caller is an Adviser and the target is not themselves.

Domain events (optional for MVP, raise if cheap):

- `UserCreated`
- `UserDisabled` / `UserEnabled`
- `CustomerReassigned` (when AdviserId changes)

Update [domain-model.md](../domain-model.md) only if further clarification is needed.

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| Users | none (already designed and introduced by Advisers) | See Advisers feature / database-design.md |

No schema change is required. This slice only inserts rows with `Role = Customer`.

Notes:

- Global unique index on `Email` already exists.
- `Users.TenantId` → Tenants FK and `Users.AdviserId` → Users FK already exist.
- `IdentityUserId` remains nullable and without a hard FK (intentional).
- EnsureCreated is still used; no EF migration in this slice unless the project switches later.

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | CreateCustomer | int (new Id) | Name, Email, AdviserId required; Email unique; Adviser valid & same tenant & enabled; Adviser caller may only target self |
| Command | UpdateCustomer | — | Name optional; IsEnabled optional; AdviserId optional but must be valid if supplied; Adviser caller may only target self |
| Command | DisableCustomer | — | Refused while the Customer has any Active Account (Accounts feature) |
| Query | GetCustomers | paginated list | page, pageSize, isEnabled?, search? + automatic role-based visibility filter |
| Query | GetCustomerById | CustomerVm (404 if missing or out of scope) | |

Scaffold from `src/Application`:

```bash
dotnet new ca-usecase --name CreateCustomer --feature-name Customers --usecase-type command --return-type int
dotnet new ca-usecase --name UpdateCustomer --feature-name Customers --usecase-type command --return-type unit
dotnet new ca-usecase --name DisableCustomer --feature-name Customers --usecase-type command --return-type unit
dotnet new ca-usecase --name GetCustomers --feature-name Customers --usecase-type query --return-type PaginatedList<CustomerVm>
dotnet new ca-usecase --name GetCustomerById --feature-name Customers --usecase-type query --return-type CustomerVm
```

CreateCustomer is a simple Domain insert (no Identity transaction required).

## 8. API

| Method | Route | Auth | Success | Errors | Description |
| --- | --- | --- | --- | --- | --- |
| GET | `/customers` | TenantAdmin, Adviser | 200 + paginated list | 401, 403 | List with pagination, `isEnabled` filter, `search`. Adviser sees only own Customers. |
| GET | `/customers/{id}` | TenantAdmin, Adviser | 200 + CustomerVm | 401, 403, 404 | Single Customer (visibility-scoped). Includes basic Adviser info. |
| POST | `/customers` | TenantAdmin, Adviser | 201 + id | 400, 401, 403 | Create Customer (no login). AdviserId required. |
| PUT | `/customers/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Update Name and/or IsEnabled and/or reassign AdviserId. |
| DELETE | `/customers/{id}` | TenantAdmin, Adviser | 204 | 400, 401, 403, 404 | Soft-disable (`IsEnabled = false`). 400 if any Active Account remains. |

Query parameters for list (identical shape to Tenants / Advisers):

- `page` (1-based, default 1)
- `pageSize` (default 20, max 100)
- `isEnabled` (optional bool)
- `search` (optional string – matches Id exactly or Name/Email contains, case-insensitive)

Request body for POST:

```json
{
  "name": "Zhang San",
  "email": "zhangsan@example.com",
  "adviserId": 12
}
```

PUT is partial: any combination of `name`, `isEnabled`, `adviserId` may be supplied. Supplying none is 400.

Suggested `CustomerVm` shape (can be refined later):

```json
{
  "id": 42,
  "name": "Zhang San",
  "email": "zhangsan@example.com",
  "isEnabled": true,
  "adviserId": 12,
  "adviserName": "Jane Smith"
}
```

Update [api-design.md](../api-design.md) in the same change if any detail needs tightening.

## 9. UI

None — API only for this slice. Customer list / create / edit pages in the Adviser Portal can be added later once the API is stable.

## 10. Tests

| Project | Cases |
| --- | --- |
| Domain.UnitTests | Customer invariants (Role / TenantId / AdviserId rules); Adviser target must be valid |
| Application.UnitTests | Create rejects duplicate Email; Adviser caller cannot assign another Adviser; non-allowed roles rejected by policy |
| Application.FunctionalTests | TenantAdmin happy path: create → list (filter/search/page) → get → update (including reassign) → disable → re-enable; Adviser can only see and manage own Customers; created Customer cannot log in |
| Web / Integration | 403 for SystemAdmin / anonymous; 404 for cross-tenant or other-Adviser id; 400 when Adviser tries to assign a Customer to a different Adviser |

## 11. Rollout

- [x] Feature spec accepted
- [x] Confirm Domain `User` invariants already cover Customer cases (no new entity)
- [x] Commands / queries / validators
- [x] Endpoints under `/customers`
- [x] Tests
- [x] Parent docs updated (domain-model if needed, api-design, function-plan, features/README)

## 12. Open questions

- None remaining after 2026-08-23 decisions.
  - Adviser callers may only assign Customers to themselves
  - Soft-disable is refused while any Active Account remains (delivered by the Accounts feature; Closed accounts do not block)
  - GET detail returns basic Customer + Adviser summary only (no Account overview yet)
  - Email remains globally unique
  - Soft-disable via DELETE, list shape, 404 scoping all consistent with Advisers

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-24 | Accounts feature delivered the Active-account disable guard: `DELETE` / `PUT isEnabled=false` returns 400 while the Customer still has Active accounts. Closed accounts do not block. |
| 2026-08-23 | Implemented. TenantAdmin + Adviser CRUD under `/customers`. Create is Domain-only (no Identity). Advisers see and assign only their own Customers (404 for others; 400 if assigning to someone else). Soft-disable via DELETE with no Account guard. `CustomerReassignedEvent` on AdviserId change. |
| 2026-08-23 | Created from discussion. Locked: Adviser self-assignment only, no Account guard on disable (deferred), global unique Email, no Identity user for Customer, soft-disable via DELETE, visibility scoping (TenantAdmin full / Adviser own only). |
