# MyWealth documentation

Shared, versioned project docs. Fill these in as decisions land. Personal scratch work belongs in [`local_docs/`](../local_docs/README.md) and is not committed.

## How the two folders differ

| Folder | Committed? | Purpose |
| --- | --- | --- |
| `docs/` | Yes | Source of truth: function plan, model, database, architecture, feature specs, ADRs |
| `local_docs/` | No (except its README) | Private drafts, research dumps, session notes, half-baked ideas |

When a local note is ready to share, copy the useful parts into the matching file under `docs/` and open a review.

## Start here

1. [Function plan](function-plan.md) — what the product does, in what order
2. [Architecture](architecture.md) — how the solution is shaped
3. [Domain model](domain-model.md) — aggregates, entities, value objects, events
4. [Database design](database-design.md) — tables, keys, indexes, migrations
5. [API design](api-design.md) — HTTP surface, auth, error shape
6. [Glossary](glossary.md) — shared terms

Per-item docs live in:

- [features/](features/README.md) — one spec per use-case / vertical slice
- [adr/](adr/README.md) — architecture decision records

Copy-paste blanks:

- [Feature spec template](_templates/feature.md)
- [ADR template](_templates/adr.md)

## Status values

Use the same `status` on every doc front matter:

| Status | Meaning |
| --- | --- |
| `draft` | Work in progress, not implementable yet |
| `review` | Ready for feedback |
| `accepted` | Agreed; implementation should follow this |
| `deprecated` | Superseded; leave in place and link to the replacement |

## Conventions

- Write for the next person implementing the slice, not for a slide deck.
- Prefer one concrete decision over a list of options. Move rejected options into an ADR.
- Keep mermaid diagrams in the doc that owns the concept. Do not duplicate the same ER diagram in three files.
- When the code and the doc disagree, either change the code or update the doc in the same change.
- Name files in kebab-case. Feature specs: `docs/features/<feature-name>.md`. ADRs: `docs/adr/NNNN-short-title.md`.

## Current codebase snapshot

The repo was generated from [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture) 10.8.0. The starter Todo / WeatherForecast sample has been removed. Identity & Auth is the first real slice.

| Layer | Project | Role |
| --- | --- | --- |
| Host | `src/AppHost` | Aspire orchestration |
| API | `src/Web` | Minimal APIs, Scalar, Identity endpoints |
| Use cases | `src/Application` | MediatR commands / queries, validators |
| Model | `src/Domain` | Entities, value objects, events |
| Persistence / identity | `src/Infrastructure` | EF Core + SQL Server, ASP.NET Identity |
| Names | `src/Shared` | Aspire resource names (`webapi`, `dbserver`, `MyWealthDb`) |
| Defaults | `src/ServiceDefaults` | Health, OTel, service discovery |
