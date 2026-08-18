# 0004. Money Representation as decimal with Currency

## Status
Accepted

## Date
2026-08-18

## Context
Monetary amounts are the core data of a wealth management platform. Floating-point types must be avoided to prevent precision errors. The design should also leave room for multi-currency support even if the initial demo uses a single currency.

## Decision
All monetary fields will use `decimal` in C# (and careful numeric handling on the frontend).  
In the database the corresponding column type will be `decimal(18,4)` or higher precision.  
A monetary value is never stored as a bare number; it is always accompanied by a currency code (Currency).  
A Money value object will be introduced in the Domain layer.

## Consequences
Positive:
- Eliminates classic floating-point precision problems (e.g. 0.1 + 0.2)
- Follows established financial-system best practices
- Makes future multi-currency support straightforward

Negative:
- Frontend must carefully handle serialization and display formatting
- Arithmetic requires currency alignment before calculation
- Extra domain modelling (Money value object) is required

## Alternatives considered
1. `double` / `float` — strictly forbidden because of precision issues
2. Integer storage of the smallest unit (e.g. cents) — viable but less intuitive than `decimal` for the current demo
3. Third-party Money libraries — overly heavy for a demo project
