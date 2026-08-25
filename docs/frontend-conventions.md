---
title: Frontend Conventions
status: draft
owner: ""
last_updated: 2026-08-26
related:
  - adviser-portal.md
  - frontend-implementation-notes.md
  - architecture.md
  - adr/0003-react-redux-typescript-vite-tailwind-frontend.md
  - features/README.md
---

# Frontend Conventions

Coding conventions for the **Adviser Portal**.  
These rules exist primarily so agentic coding produces consistent, reviewable code. Keep the document short; prefer updating it when a real decision is made rather than inventing rules in advance.

## 1. Purpose

- Give a single source of truth for folder layout, naming, state, API access, and routing patterns.
- Make step-by-step agent implementations predictable and easy to commit.
- Stay lightweight — do not duplicate the page inventory or role menus already defined in [adviser-portal.md](adviser-portal.md).

## 2. Folder structure (mandatory)

```
src/
├── app/                       # store, typed hooks, router, providers only
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
├── shared/                    # reusable UI, utils, types, api client
│   ├── api/
│   ├── components/
│   ├── hooks/
│   ├── types/
│   └── utils/
├── layouts/                   # MainLayout, etc.
├── App.tsx
└── main.tsx
```

Rules:

- Feature code lives under `features/<name>/`. Do not put feature pages in a global `pages/` folder.
- Cross-cutting UI and helpers belong in `shared/`.
- `app/` stays thin: store configuration, typed hooks, and the router definition only.

Suggested layout inside a feature:

```
features/customers/
├── api.ts              # optional RTK Query endpoints or fetch helpers for this feature
├── customersSlice.ts   # if needed
├── components/         # feature-specific components
├── CustomerListPage.tsx
├── CustomerDetailPage.tsx
└── index.ts            # public exports if useful
```

## 3. Naming conventions

| Kind | Convention | Example |
| --- | --- | --- |
| Page components | PascalCase + `Page` suffix | `CustomerListPage.tsx` |
| Layout components | PascalCase + `Layout` | `MainLayout.tsx` |
| Shared UI components | PascalCase | `Button.tsx`, `DataTable.tsx` |
| Redux slices | camelCase + `Slice` | `authSlice.ts` |
| RTK Query APIs | camelCase + `Api` | `customersApi.ts` |
| Typed hooks | `use` + descriptive name | `useAppDispatch`, `useCurrentUser` |
| Files | Match the main export | `CustomerListPage.tsx` exports `CustomerListPage` |

- Prefer named exports for components and utilities.
- One main component per file for pages and significant UI pieces.

## 4. State management

- **Redux Toolkit** is the default for shared client state.
- Always use the typed hooks from `app/hooks.ts` (`useAppDispatch`, `useAppSelector`, `useAppStore`). Never use the plain `useDispatch` / `useSelector` in feature code.
- **Server state**: prefer RTK Query (or a thin equivalent pattern) over hand-written thunks + local loading flags when the data is fetched from the API.
- Put only genuinely shared state in the store:
  - Current user / auth status
  - Global UI flags that cross features
- Local UI state (form fields, open/closed modals, temporary filters) stays in the component with `useState` / `useReducer` unless it must survive navigation.
- Each feature registers its own slice/API under `features/<name>/` and is added to `app/store.ts`.

## 5. API client conventions

- All HTTP calls go through a shared client in `shared/api/` (or RTK Query base API).
- Base URL is read from environment / Aspire service discovery (do not hard-code production hosts).
- Attach `Authorization: Bearer <token>` in a single place (request interceptor or RTK Query `prepareHeaders`).
- On **401**:
  1. Clear the stored token
  2. Clear current-user state in Redux
  3. Redirect to `/login`
- Prefer typed responses. Until OpenAPI generation is wired, hand-maintain types under `shared/types/` or next to the feature API module.
- Do not call `fetch` / `axios` directly from page components; go through the shared client or a feature API module.

## 6. Routing & auth (implementation rules)

Aligned with [adviser-portal.md](adviser-portal.md):

- Use the Data Router (`createBrowserRouter` + `RouterProvider`).
- Define routes in `app/router.tsx`.
- `/login` is public.
- All other MVP routes sit under a protected layout.
- Unauthenticated access → redirect to `/login`.
- Authenticated access to `/login` → redirect to `/` (Dashboard).
- Dashboard (`/`) is the default post-login page.
- JWT is stored in `localStorage` for this demo.
- After login, call `GET /users/me` and store the result in Redux; role-based menu visibility is driven by that payload.

Protected route pattern:

- A layout route (`MainLayout`) checks for a valid token / current user.
- Child routes render inside `<Outlet />`.
- Menu visibility and URL access both come from `NAV_ITEMS` + `currentUser.role`. A matching nav item the user cannot see redirects to `/`.
- There is still no dedicated 403 page. Add one later if a feature needs an explicit “no access” screen.

## 7. UI & Tailwind conventions

- **Raw Tailwind** is the MVP styling approach. No component library (shadcn or otherwise) until an explicit decision is recorded here.
- Prefer composition of small shared components (`shared/components/`) over giant page-level class strings when the same pattern repeats.
- `PageHeader` is the first shared primitive; use it for page titles.
- Keep visual design simple and consistent for a demo:
  - Clear page titles
  - Standard spacing scale
  - Primary actions obvious
- Do not introduce a second CSS approach (CSS Modules, styled-components, etc.) unless an explicit decision is recorded.
- Accessibility basics: form labels, button types, skip-to-content, and sensible focus order.

## 8. Agentic coding discipline

When generating an implementation plan for a frontend slice:

- Break work into small, sequential steps.
- Explicitly mark each step that should produce a **git commit**.
- Prefer commits that leave the app in a runnable state (`npm run dev` still works).
- Do not mix unrelated features in one commit.
- Follow the existing order in [adviser-portal.md](adviser-portal.md) §8 unless there is a documented reason to deviate.
- After implementing a page, update the corresponding Backend Feature Spec Section 9 (UI) if meaningful UI decisions were made, or leave a short note in this document / `adviser-portal.md`.

Example step markers an agent should emit:

```
Step 1 → create authSlice + token helpers
Step 2 → commit: add auth slice and typed hooks usage
Step 3 → LoginPage + form
Step 4 → commit: add Login page
...
```

## 9. What not to do

- Do not create a parallel set of heavy frontend Feature Specs for every page.
- Do not put business rules only in the UI — backend remains the source of truth.
- Do not invent new top-level folders without updating this document and `adviser-portal.md`.
- Do not hard-code role names or menu structures in multiple places; derive visibility from the current user payload and the table in `adviser-portal.md`.

## 10. Changelog

| Date | Change |
| --- | --- |
| 2026-08-26 | Recorded raw Tailwind for MVP; URL-level role guard (redirect home, no 403 page); `PageHeader` as first shared primitive. |
| 2026-08-26 | Linked Frontend Implementation Notes. |
| 2026-08-25 | Initial draft for agentic coding readiness. |
