---
title: "<Feature name>"
status: draft
owner: ""
last_updated: YYYY-MM-DD
related:
  - ../function-plan.md
  - ../domain-model.md
  - ../database-design.md
  - ../api-design.md
---

# <Feature name>

One vertical slice: user-visible behaviour + the Domain / Application / Web / DB changes that ship together.

## 1. Summary

<!-- Two or three sentences. Who is this for, what can they do afterwards? -->

## 2. Scope

**In**

- 

**Out**

- 

## 3. User stories

1. As a … I want … so that …
2. 

## 4. Rules

| ID | Rule |
| --- | --- |
| R1 | |
| R2 | |

## 5. Domain

| Type | Kind | Notes |
| --- | --- | --- |
| | aggregate / entity / VO / event | |

Invariants:

- 

Domain events:

- 

Update [domain-model.md](../domain-model.md) in the same change.

## 6. Database

| Table | Change | Indexes / FKs |
| --- | --- | --- |
| | add / alter / drop | |

Migration name: `<timestamp>_<name>`

Update [database-design.md](../database-design.md) in the same change.

## 7. Application use cases

| Kind | Name | Returns | Validator highlights |
| --- | --- | --- | --- |
| Command | | | |
| Query | | | |

Scaffold from `src/Application`:

```bash
dotnet new ca-usecase --name <Name> --feature-name <Feature> --usecase-type command --return-type int
dotnet new ca-usecase -n <Name> -fn <Feature> -ut query -rt <Vm>
```

## 8. API

| Method | Route | Auth | Success | Errors |
| --- | --- | --- | --- | --- |
| GET | | user | 200 | 401 |
| POST | | user | 201 | 400, 401 |
| PUT | | user | 204 | 400, 401, 404 |
| DELETE | | user | 204 | 401, 404 |

Update [api-design.md](../api-design.md) in the same change.

## 9. UI

<!-- Page / component, empty state, error state. "None — API only" is fine. -->

## 10. Tests

| Project | Cases |
| --- | --- |
| Domain.UnitTests | |
| Application.UnitTests | |
| Application.FunctionalTests | |

## 11. Rollout

- [ ] Feature spec accepted
- [ ] Domain types
- [ ] EF configuration + migration
- [ ] Commands / queries / validators
- [ ] Endpoints
- [ ] Tests
- [ ] Parent docs updated (model, DB, API, function plan)

## 12. Open questions

- 

## 13. Changelog

| Date | Change |
| --- | --- |
| | Created |
