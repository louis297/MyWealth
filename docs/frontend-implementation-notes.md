---
title: Frontend Implementation Notes
status: draft
owner: ""
last_updated: 2026-08-26
related:
  - adviser-portal.md
  - frontend-conventions.md
  - architecture.md
  - api-design.md
  - adr/0003-react-redux-typescript-vite-tailwind-frontend.md
  - features/identity-auth.md
  - features/dashboard.md
  - features/customers.md
  - features/accounts.md
  - features/holdings.md
  - features/transactions.md
  - features/advisers.md
---

# Frontend Implementation Notes

Practical implementation guidance for the **Adviser Portal** (MVP).  

This document is intentionally lightweight. It does **not** create a parallel set of heavy frontend Feature Specs. Read it together with:

- [adviser-portal.md](adviser-portal.md) — page inventory, role-based navigation, routing conventions
- [frontend-conventions.md](frontend-conventions.md) — folder structure, naming, state, API client, agentic discipline

Individual Backend Feature Specs remain the source of truth for business rules and API contracts. Section 9 (UI) in those specs is usually marked “None — API only”; this file fills the gap for the frontend team / agents.

## 0. Shared infrastructure (do first)

Goal: every subsequent page can call the API safely and sits inside a consistent shell.

### Project scaffold

- Vite + React + TypeScript + Redux Toolkit + React Router (Data Router style) + Tailwind CSS
- Resource name in Aspire: `adviser-portal` (never generic names such as `frontend`)

### Required pieces

| Piece | Location | Notes |
| --- | --- | --- |
| Store | `app/store.ts` | Register feature slices / RTK Query APIs here |
| Typed hooks | `app/hooks.ts` | Always use `useAppDispatch` / `useAppSelector` / `useAppStore` |
| Router | `app/router.tsx` | Data Router; `/login` public, everything else protected |
| API client | `shared/api/` | Base URL from env / Aspire service discovery. Attach Bearer token in one place. On 401: clear token + current-user → redirect `/login` |
| Auth state | Redux (auth / currentUser) | JWT in `localStorage` (acceptable for this demo). After login call `GET /users/me` and store the result. Role, display name and tenantId come from this payload — do not parse the JWT on the client |

### Routing & auth rules (recap)

- `/login` is the only public route in MVP
- Unauthenticated access to a protected route → redirect to `/login`
- Authenticated access to `/login` → redirect to `/` (Dashboard)
- Dashboard (`/`) is the default post-login landing page

### MainLayout

- Left sidebar + top bar (current user display name + role)
- Menu visibility driven exclusively by the role table in [adviser-portal.md](adviser-portal.md) §4 and the payload from `/users/me`
- Do not hard-code role checks in multiple places

## 1. Auth

**Pages:** Login (public), later Profile reuses the same current-user state.

**Key endpoints:**

| Method | Route | Notes |
| --- | --- | --- |
| POST | `/auth/login` | Returns token. Customer role → 403 |
| POST | `/auth/logout` | 204 |
| GET | `/users/me` | CurrentUserVm (id, email, displayName, role, tenantId, …) |
| PUT | `/users/me` | Non-password profile fields only (e.g. displayName) |
| PUT | `/users/me/password` | Requires current password + new password |

**Implementation notes:**

- Login form: email + password
- Success path: store token → call `/users/me` → store currentUser → navigate to `/`
- Treat 403 on login the same as ordinary credential failure for UX (simple “invalid credentials or no access” message is fine for the demo)
- 401 handling is already centralised in the shared API client
- Logout: call endpoint + clear local state + navigate to `/login`

**Suggested commits:**

1. auth slice + token helpers + 401 interceptor
2. Login page
3. ProtectedRoute + post-login `/users/me` load

## 2. MainLayout + role-based navigation

- Drive menu items from the role table in [adviser-portal.md](adviser-portal.md)
- SystemAdmin surface in MVP is intentionally thin (Dashboard + Profile only)
- TenantAdmin sees Advisers menu; Adviser does not
- Highlight the active route
- Top bar shows current user’s display name and role

This shell must be stable before building business pages.

## 3. Dashboard (default home)

**Route:** `/`

**Key endpoints:**

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/dashboard/net-worth` | Optional `?customerId=` |
| GET | `/dashboard/allocation` | Optional `?customerId=` |

Both return multi-currency arrays. Empty data → empty array.

**Implementation notes:**

- Two distinct sections: Net Worth and Asset Allocation
- Render one card / block per currency
- Closed accounts are already excluded by the backend
- No historical charts, no export, no advanced filters in MVP
- Optional customer filter can be added later; start with the global view

**Empty / error states:**

- Empty array → friendly “No data yet” message
- Network / 401 handled by shared client

**Suggested approach:** static layout first, then wire the two endpoints, then polish numbers and empty states.

## 4. Customers

**Pages:** list + detail

**Key endpoints:**

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/customers` | Pagination, `search`, `isEnabled`. Advisers see only their own customers |
| GET | `/customers/{id}` | Visibility-scoped; out-of-scope → 404 |
| POST | `/customers` | Creates Domain User only (no login). `adviserId` required |
| PUT | `/customers/{id}` | Partial: name / isEnabled / adviserId |
| DELETE | `/customers/{id}` | Soft-disable. 400 if any Active Account remains |

**Implementation notes:**

- List: search (name / email) + enabled filter + pagination
- Create: Adviser callers may only assign themselves; TenantAdmin may assign any enabled Adviser in the tenant
- Detail: basic info + assigned Adviser; optionally show related accounts via `GET /accounts?customerId=`
- Soft-disable requires confirmation; surface the backend 400 message when Active accounts still exist

**Empty state:** “No customers yet” with a clear create action.

**Suggested order:** read-only list + detail first, then create / edit / disable.

## 5. Accounts (including Holdings surface)

**Pages:** account list (top-level menu) + account detail (Holdings live here)

**Key endpoints — Accounts:**

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/accounts` | Filters: `status`, `customerId`, `search` |
| GET | `/accounts/{id}` | |
| POST | `/accounts` | Currency becomes immutable |
| PUT | `/accounts/{id}` | Name and/or Type only |
| POST | `/accounts/{id}/close` | Permanent. Empty body |

**Key endpoints — Holdings (nested):**

| Method | Route | Notes |
| --- | --- | --- |
| GET/POST | `/accounts/{accountId}/holdings` | |
| GET/PUT/DELETE | `/accounts/{accountId}/holdings/{id}` | Physical DELETE refused when historical Transactions exist |

**Implementation notes:**

- Accounts is a first-class menu item (not only reachable via Customer drill-down)
- Create form: Customer + Name + Type + Currency
- Close is irreversible → strong confirmation dialog
- Closed accounts reject new Holdings and Transactions (backend)
- Account detail shows Holdings table (quantity, cost basis, instrument)
- Holdings CostBasis.Currency must match the Account currency (backend enforces)
- Do **not** create a separate top-level Holdings menu

**Suggested commits:**

1. Accounts list + detail (read-only)
2. Create / edit name+type / close
3. Holdings CRUD embedded in the detail page

## 6. Transactions

**Pages:** list + create (page, drawer or modal)

**Key endpoints:**

| Method | Route | Notes |
| --- | --- | --- |
| GET | `/transactions` | Filters: `accountId`, `from`, `to`, `type` + pagination |
| GET | `/transactions/{id}` | |
| POST | `/transactions` | Returns `{ id }` only. Append-only |

**Implementation notes:**

- List supports the filters above
- Create flow:
  - Select Account first
  - Type determines whether Holding + Quantity are required (Buy / Sell yes; cash types no)
  - Amount is always positive; direction is expressed solely by Type
  - `BookedOn` may be a future date
  - Currency must match the Account
- After successful create the client may re-fetch the related Holding or Account if updated positions are needed
- No edit or delete UI (append-only)

**Empty state:** simple “No transactions” message.

**Suggested order:** list first, then create form (start with the most common types).

## 7. Profile + Advisers

### Profile (all authenticated roles)

- Display data from `/users/me`
- Update display name → `PUT /users/me`
- Change password → separate form calling `PUT /users/me/password` (current + new password)

### Advisers (TenantAdmin only)

**Key endpoints:** `/advisers` (list, get, create, update, soft-disable)

- Create also creates the Identity login; password is supplied by the caller
- Soft-disable fails if any Customer is still assigned (backend 400)
- Email is globally unique (demo simplification)

SystemAdmin Tenant / TenantAdmin management screens remain out of MVP UI (use Scalar / API).

## 8. Suggested implementation order & commit discipline

Follow the order already locked in [adviser-portal.md](adviser-portal.md) §8 and the agentic rules in [frontend-conventions.md](frontend-conventions.md):

1. Scaffold + store + typed hooks + router + shared API client
2. Auth end-to-end (Login, token, `/users/me`, 401, ProtectedRoute)
3. MainLayout + role-based menu
4. Dashboard
5. Customers list & detail
6. Accounts list & detail (including Holdings)
7. Transactions list & create
8. Profile + Advisers
9. Polish and remaining P2 items

Rules for agents:

- Break work into small sequential steps
- Explicitly mark every step that should produce a **git commit**
- Prefer commits that leave the app runnable (`npm run dev` still works)
- Do not mix unrelated features in one commit

## 9. Cross-cutting notes

- Money is always `{ amount: decimal, currency: string }`. The frontend displays; it does not re-calculate business totals.
- Pagination shape is the standard paginated response used across the API (`items`, `pageNumber`, `totalPages`, `totalCount`, …).
- 404 means “not found or not visible to you”. Do not try to distinguish cross-tenant cases.
- Prefer pure Tailwind for MVP. If a component library (or thin primitives) is introduced later, record the decision in [frontend-conventions.md](frontend-conventions.md).
- This document covers only the Adviser Portal MVP. Customer Portal and advanced admin screens are explicitly out of scope.

## 10. Changelog

| Date | Change |
| --- | --- |
| 2026-08-26 | Initial draft derived from the Chinese pure-text discussion. Aligns with adviser-portal page inventory and frontend-conventions. |
