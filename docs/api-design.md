---
title: API design
status: draft
owner: ""
last_updated: 2026-08-16
related:
  - architecture.md
  - function-plan.md
  - features/README.md
---

# API design

HTTP surface of `webapi`. Endpoint groups live in `src/Web/Endpoints`. Each group implements `IEndpointGroup` and is discovered by `MapEndpoints`.

## 1. Conventions

| Topic | Convention |
| --- | --- |
| Style | Minimal APIs, typed results (`Ok`, `Created`, `NoContent`, `BadRequest`, …) |
| Dispatch | Endpoint sends a MediatR command/query. No DbContext in endpoints. |
| Auth default | `groupBuilder.RequireAuthorization()` on the group, unless it is the Identity group |
| Route | Group name ≈ type name (`TodoLists` → `/TodoLists`). Prefer kebab-case for new groups (`/accounts`) — pick one and ADR it. |
| Ids | Route `{id}` must match the command's `Id` on updates; otherwise `400` |
| Create | `201 Created` with the new int id |
| Update / delete | `204 NoContent` |
| Get one / list | `200 Ok` + view model |
| Validation | FluentValidation → `ValidationException` → problem details |
| Forbidden | `ForbiddenAccessException` |
| Docs | `[EndpointSummary]` + `[EndpointDescription]` on every action |
| Explore | Scalar at `/scalar` (root `/` redirects there) |

## 2. Current endpoints (starter)

| Group | Methods | Auth | Command / query |
| --- | --- | --- | --- |
| `Users` | Identity API (`MapIdentityApi<ApplicationUser>`) + `POST /users/logout` | logout requires auth | Identity |
| `TodoLists` | `GET /` `POST /` `PUT /{id}` `DELETE /{id}` | required | GetTodos, Create/Update/DeleteTodoList |
| `TodoItems` | create / update / update-detail / delete | required | matching commands |
| `WeatherForecasts` | `GET` | _check group_ | `GetWeatherForecastsQuery` |

Identity routes come from `MapIdentityApi` (register, login, refresh, …). Confirm the exact paths in Scalar after `aspire start`.

## 3. Auth

- Scheme: bearer tokens (`AddBearerToken(IdentityConstants.BearerScheme)`).
- Current user: `IUser` / `Web/Services/CurrentUser.cs`.
- Per-request roles: `[Authorize(Roles = Roles.Administrator)]` on the command/query type, enforced by `AuthorizationBehaviour`.

New resource endpoints must:

1. Require authorization on the group.
2. Scope every query to the current user (`IUser.Id`).
3. Return 404 (not 403) when the id exists but is owned by someone else — unless an ADR says otherwise.

## 4. Resource catalog (fill in)

One row per future group. Link the feature spec when it exists.

| Resource | Base route | Methods | Auth | Feature spec |
| --- | --- | --- | --- | --- |
| Accounts | `/accounts` | GET list, GET id, POST, PUT, DELETE | user | |
| Holdings | `/accounts/{accountId}/holdings` | GET, POST, PUT, DELETE | user | |
| Transactions | `/transactions` | GET (filter), POST, PUT, DELETE | user | |
| Categories | `/categories` | GET, POST, PUT, DELETE | user | |
| Net worth | `/net-worth` | GET | user | |
| Users | `/users` | Identity + logout | mixed | (exists) |

## 5. Error shape

Describe the payload clients should handle. Fill in once the exception handler is documented or changed.

| Situation | HTTP | Body |
| --- | --- | --- |
| Validation failure | 400 | Validation problem details (FluentValidation errors) |
| Unauthenticated | 401 | |
| Authenticated but not allowed | 403 | |
| Missing / other-user resource | 404 | |
| Unhandled | 500 | exception handler |

## 6. Versioning and clients

- No API versioning yet.
- OpenAPI is generated at runtime (`MapOpenApi`). Generated client path `**/web-api-client.ts` is gitignored.
- CORS is wide open (`AllowAnyOrigin`). Tighten before any real frontend talks to a non-local API.

## 7. Endpoint checklist

When adding a group:

- [ ] New file `src/Web/Endpoints/<Name>.cs` implementing `IEndpointGroup`
- [ ] `[EndpointSummary]` / `[EndpointDescription]`
- [ ] Command/query + validator in Application
- [ ] Feature spec in `docs/features/`
- [ ] Functional tests in `tests/Application.FunctionalTests`
- [ ] Row added to the catalog above

## 8. Changelog

| Date | Change |
| --- | --- |
| 2026-08-16 | Template created; starter endpoint groups listed |
