# 0007. BaseEntity Primary Key Uses int (Identity)

## Status
Accepted

## Date
2026-08-18

## Context
All business entities need a consistent primary-key strategy. The choice of key type affects database design, API routing, front-end display, relationships and future scalability.

## Decision
The primary key of `BaseEntity` will be a database-generated `int` (Identity / auto-increment).

## Consequences
Positive:
- Compact keys, efficient indexes and good read/write performance
- Cleaner URLs and front-end display (e.g. `/accounts/123`)
- Matches traditional relational-database conventions and has low learning cost
- Mature support in EF Core

Negative:
- In a distributed environment key generation would need extra handling (not an issue for the current single-database demo)
- Sequential keys can be guessed, presenting a minor information-disclosure risk (acceptable for a demo)
- Future data merges from multiple systems could produce key collisions

## Alternatives considered
1. `Guid` (UUID) — globally unique and suitable for distributed systems, but longer keys, slightly worse index performance and less friendly URLs
2. `long` (bigint) — larger range, unnecessary for the data volume of this demo
3. Custom algorithms (Snowflake, etc.) — implementation complexity too high for a demo
