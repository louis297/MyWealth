---
title: "Identity & Auth"
status: draft
owner: ""
last_updated: 2026-08-21
related:
  - ../function-plan.md
  - ../domain-model.md
  - ../database-design.md
  - ../api-design.md
  - ../adr/0006-email-password-jwt-authentication.md
---

# Identity & Auth

Foundation vertical slice for authentication, current-user profile, and role-based login rules. All subsequent features depend on a stable JWT + `IUser` / `CurrentUser` abstraction.

## 1. Summary

Any login-capable user (SystemAdmin, TenantAdmin, Adviser) can sign in with email + password, receive a JWT, view and update their own non-password profile, and change their password. Customer role accounts are deliberately blocked from authenticating in MVP. The frontend stores the JWT and calls `/users/me` when it needs the current principal.

## 2. Scope

**In**

- Email + password login that issues a JWT
- Logout
- `GET /users/me` — current user profile
- `PUT /users/me` — update non-password profile fields (display name and any future non-sensitive fields)
- `PUT /users/me/password` — change password (requires current password)
- Role claims and TenantId claim inside the JWT
- Explicit rejection of Customer-role login (403)
- `IUser` / `CurrentUser` abstraction used by the rest of the application

**Out**

- User registration (handled by the `advisers`, `customers`, and `tenants` features when those entities are created)
- Password reset / forgot-password flow
- Refresh tokens
- Third-party / social login
- Avatar upload
- Any business-data CRUD
- Customer Portal login (future)

## 3. User stories

1. As a TenantAdmin / Adviser / SystemAdmin I want to log in with email and password so that I can access the Adviser Portal.
2. As a logged-in user I want to see my own profile (name, email, role, tenant) so that the UI can personalise the shell.
3. As a logged-in user I want to update my display name (and other non-password profile fields) so that my identity is correct.
4. As a logged-in user I want to change my password so that I can keep the account secure.
5. As the system I must reject any login attempt by a Customer-role account so that the MVP boundary is enforced.

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | Only SystemAdmin, TenantAdmin and Adviser may authenticate. Customer role always receives 403 on login. |
| R2 | JWT must contain at least: user id, email, role, tenantId (null for SystemAdmin). |
| R3 | `PUT /users/me` may change only non-password profile fields. Email, Role, TenantId and IsEnabled are immutable through this endpoint. |
| R4 | `PUT /users/me/password` requires the current password and a new password that satisfies Identity password rules. |
| R5 | Logout invalidates the current session / token according to the Identity configuration used. |
| R6 | All endpoints except login require a valid JWT. |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| ApplicationUser | Infrastructure entity (Identity) | Lives in Infrastructure. Extended with Role, TenantId, DisplayName, IsEnabled, AdviserId, etc. Domain only ever sees a string UserId. |
| — | — | No new Domain aggregate is introduced by this feature. |

Invariants:

- A user with Role = Customer must never be issued a valid authentication token.
- Profile updates never change Role, TenantId, Email or IsEnabled.

Domain events:

- None required for MVP.

Update [domain-model.md](../domain-model.md) only if new terminology appears; the existing language already covers User / roles / “Customer cannot authenticate”.

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| AspNetUsers (ApplicationUser) | Ensure columns exist: DisplayName (or FullName), Role, TenantId, IsEnabled, AdviserId | Existing Identity indexes + any needed for TenantId / Role |

Migration name: (none specific if the columns already exist from the Identity + multi-tenancy setup; otherwise a small additive migration)

Update [database-design.md](../database-design.md) if any new column is introduced.

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | LoginCommand | LoginResultVm (token + basic claims) | Email required, Password required; after Identity succeeds, reject if Role == Customer |
| Command | LogoutCommand | — | — |
| Query | GetCurrentUserQuery | CurrentUserVm | — |
| Command | UpdateCurrentUserCommand | — | DisplayName (or other allowed fields) required / length rules |
| Command | ChangePasswordCommand | — | CurrentPassword required, NewPassword required + Identity strength rules |

Scaffold from `src/Application` (adjust names to match existing conventions):

```bash
dotnet new ca-usecase -n Login -fn IdentityAuth -ut command -rt LoginResultVm
dotnet new ca-usecase -n GetCurrentUser -fn IdentityAuth -ut query -rt CurrentUserVm
dotnet new ca-usecase -n UpdateCurrentUser -fn IdentityAuth -ut command -rt unit
dotnet new ca-usecase -n ChangePassword -fn IdentityAuth -ut command -rt unit
```

`IUser` / `CurrentUser` implementation lives under `src/Web` (or Shared) and is injected into Application behaviours / handlers that need the caller identity.

## 8. API

| Method | Route | Auth | Success | Errors |
| --- | --- | --- | --- | --- |
| POST | `/auth/login` | anonymous | 200 + token payload | 400 (validation), 401 (bad credentials), **403 (Customer role)** |
| POST | `/auth/logout` | user | 204 | 401 |
| GET | `/users/me` | user | 200 + CurrentUserVm | 401 |
| PUT | `/users/me` | user | 204 | 400, 401 |
| PUT | `/users/me/password` | user | 204 | 400, 401 |

Notes:

- Login is deliberately **not** under `/users` so that the auth surface stays distinct from the current-user resource.
- This feature **introduces** `/users/me`, reversing the earlier “no /me” decision in api-design.md. That document must be updated in the same change set.
- Customer login failure is **403 Forbidden** (not 401), because the credentials may be valid but the role is not allowed to authenticate.

Update [api-design.md](../api-design.md) in the same change.

## 9. UI

Adviser Portal pages that belong to this slice:

- Login page (email + password form, error states for 401 / 403)
- Profile page / drawer (view + edit non-password fields)
- Change-password form (current + new + confirm)

Empty / error states should surface the 403 “Customer accounts cannot sign in” message clearly when relevant.

## 10. Tests

| Project | Cases |
| --- | --- |
| Application.UnitTests | Login rejects Customer role; UpdateCurrentUser cannot change Role/TenantId/Email; ChangePassword validates current password |
| Application.FunctionalTests | Full login → receive token → call `/users/me` → update profile → change password → logout |
| Web / Integration | 403 returned for Customer credentials; anonymous access to `/users/me` returns 401 |

## 11. Rollout

- [ ] Feature spec accepted
- [ ] Confirm ApplicationUser columns (DisplayName, Role, TenantId, \ldots)
- [ ] Login / Logout / GetCurrentUser / UpdateCurrentUser / ChangePassword handlers + validators
- [ ] Endpoints under `/auth` and `/users/me`
- [ ] `IUser` / `CurrentUser` wired
- [ ] Tests
- [ ] Parent docs updated (especially api-design.md — remove “no /me”, document the new routes and the 403 Customer rule)

## 12. Open questions

- Exact property name on ApplicationUser for the display name (`DisplayName` vs `FullName`) — align with database-design.md when the first migration lands.
- Whether logout should also blacklist the JWT (MVP can rely on short-lived tokens + client-side discard).

## 13. Changelog

| Date | Change |
| --- | --- |
| 2026-08-21 | Created from discussion. Custom `/auth` routes, `/users/me` introduced, Customer login → 403, profile vs password split. |
