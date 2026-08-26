---
title: Adviser Portal
status: draft
owner: ""
last_updated: 2026-08-26
related:
  - architecture.md
  - function-plan.md
  - api-design.md
  - frontend-conventions.md
  - frontend-implementation-notes.md
  - adr/0003-react-redux-typescript-vite-tailwind-frontend.md
  - features/identity-auth.md
  - features/dashboard.md
  - features/customers.md
  - features/accounts.md
  - features/holdings.md
  - features/transactions.md
  - features/advisers.md
---

# Adviser Portal

Frontend planning for the MVP **Adviser Portal**. This is a lightweight document that locks the overall structure, page scope, navigation, routing/auth conventions, and folder layout. It does **not** replace individual Feature Specs.

Individual feature UI details remain optional in their Feature Specs (many are currently marked “None — API only”). Update this document when the portal’s information architecture, navigation model, or cross-cutting frontend conventions change.

## 1. Scope

- **In MVP**: Adviser Portal only.
- **Out of MVP**: Customer Portal, Back Office / admin portal.
- The portal is an independent React application hosted by Aspire under the resource name `adviser-portal` (never generic names such as `frontend` or `webfrontend`).
- Communication with the backend is exclusively via JWT-protected REST APIs.

## 2. Technology stack

Locked by [ADR 0003](adr/0003-react-redux-typescript-vite-tailwind-frontend.md):

- React + Redux Toolkit + TypeScript
- Vite
- Tailwind CSS
- React Router (Data Router style)

## 3. MVP page inventory

### P0 — Must have

| Page | Notes |
| --- | --- |
| Login | Email + password. Public route. |
| Dashboard | Default landing page after login. Net Worth + Allocation. |
| Customers (list + detail) | List with search/filter; detail shows basic info + related accounts overview. |
| Accounts (list + detail) | Independent top-level menu. Detail surfaces Holdings overview. |
| Transactions (list + create) | List with filters; create form for supported transaction types. |

### P1 — Immediately after P0

| Page | Notes |
| --- | --- |
| Profile | Update display name; change password (separate from profile fields). |
| Advisers (list + create/edit) | TenantAdmin only. |
| Account create / edit name+type / close | Close is permanent. |

### P2 — Can be deferred

| Page | Notes |
| --- | --- |
| Audit Log | Optional for first usable demo. |

### Explicitly out of MVP UI

- SystemAdmin Tenant management pages (use Scalar / API for now)
- SystemAdmin TenantAdmin management pages (use Scalar / API for now)
- Any Customer login / Customer Portal screens
- Historical Net Worth charts
- Report export (CSV / PDF)
- Advanced filtering or bulk operations beyond what the current APIs support

## 4. Role-based navigation

| Menu item        | SystemAdmin | TenantAdmin | Adviser |
|------------------|-------------|-------------|---------|
| Dashboard        | ✓           | ✓           | ✓       |
| Customers        |             | ✓           | ✓       |
| Accounts         |             | ✓           | ✓       |
| Transactions     |             | ✓           | ✓       |
| Advisers         |             | ✓           |         |
| Profile          | ✓           | ✓           | ✓       |
| Logout           | ✓           | ✓           | ✓       |

Notes:

- SystemAdmin currently has a very thin UI surface in MVP (Dashboard + Profile). Tenant / TenantAdmin administration stays API-only.
- Accounts and Holdings are reachable as a first-class menu (not only via Customer drill-down).
- Menu visibility is driven by the role returned from `/users/me`.

## 5. Routing & authentication conventions

- Use React Router **Data Router** (`createBrowserRouter` + `RouterProvider`).
- `/login` is the only public route in MVP.
- All other routes live under a protected layout that requires a valid JWT.
- Unauthenticated access to a protected route → redirect to `/login`.
- Authenticated access to `/login` → redirect to Dashboard (`/`).
- Dashboard (`/`) is the default post-login landing page.
- JWT storage: `localStorage` (acceptable for this demo).
- API client attaches `Authorization: Bearer <token>` on every request.
- On 401: clear token, clear current-user state, redirect to `/login`.
- Current user: after successful login, call `GET /users/me` and store the result in Redux. Role and display name come from this payload.

## 6. Folder structure convention

```
src/
├── app/                     # store, typed hooks, router, providers
│   ├── store.ts
│   ├── hooks.ts
│   └── router.tsx
├── features/
│   ├── auth/
│   ├── dashboard/
│   ├── customers/
│   ├── accounts/
│   ├── transactions/
│   └── advisers/
├── shared/                  # reusable UI, utils, types, api client
├── layouts/                 # MainLayout, AuthLayout
├── App.tsx                  # or root layout shell
└── main.tsx
```

Principles:

- Feature folders own their pages, slices, and feature-specific components.
- Cross-cutting UI and helpers live in `shared/`.
- Keep the root `app/` thin (store, router, providers only).

## 7. State management conventions

- Redux Toolkit for global client state.
- Prefer RTK Query (or a thin API slice pattern) for server state.
- Each feature registers its own slice(s) under `features/<name>/`.
- Only put genuinely shared state in the store (current user, auth status, global UI flags). Local component state stays local.
- Always use the typed hooks (`useAppDispatch`, `useAppSelector`) instead of the plain React-Redux hooks.

## 8. Suggested implementation order

1. Project scaffold + Redux store + typed hooks + React Router (done)
2. Auth end-to-end (Login page, token handling, interceptor, ProtectedRoute, `/users/me`) (done)
3. MainLayout (sidebar + top bar + role-based menu) (done)
4. Dashboard (Net Worth + Allocation) (done)
5. Customers list & detail (done)
6. Accounts list & detail (including Holdings surface) (done)
7. Transactions list & create
8. Profile + Advisers management
9. Polish and remaining P2 items

## 9. Related documents

- [Architecture](architecture.md) — hosting model and Aspire resource naming
- [Function Plan](function-plan.md) — original MVP feature intent
- [API Design](api-design.md) — endpoints the portal will call
- [Frontend Conventions](frontend-conventions.md) — coding rules and agentic discipline
- [Frontend Implementation Notes](frontend-implementation-notes.md) — practical page-by-page implementation guidance
- [Identity & Auth Feature Spec](features/identity-auth.md)
- [Dashboard Feature Spec](features/dashboard.md)

## 10. Changelog

| Date       | Change |
| ---------- | ------ |
| 2026-08-26 | Accounts implemented: list + create + detail (holdings CRUD), edit name/type, confirm-and-close. |
| 2026-08-26 | Customers implemented: list + create + detail (accounts overview), edit, confirm-and-disable. |
| 2026-08-26 | Dashboard home implemented: Net Worth + Allocation (global view), SystemAdmin message. |
| 2026-08-26 | MainLayout implemented: role-based menu, AuthLayout, URL guard, 404, mobile drawer. |
| 2026-08-26 | Linked Frontend Implementation Notes. |
| 2026-08-25 | Initial draft. Locked: Dashboard as default home; Accounts as independent menu; SystemAdmin Tenant/TenantAdmin pages out of MVP UI. |
