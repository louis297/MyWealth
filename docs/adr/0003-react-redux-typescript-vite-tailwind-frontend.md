# 0003. Frontend: React + Redux + TypeScript + Vite + Tailwind CSS

## Status
Accepted

## Date
2026-08-18

## Context
The frontend must present a wealth dashboard, account lists, transaction history, asset allocation views, etc. Requirements include clear state management, type safety, good developer experience, and a lightweight yet maintainable styling approach.

## Decision
The frontend technology stack is:
- React + Redux Toolkit + TypeScript
- Vite as the build tool
- Tailwind CSS for styling

The frontend is fully decoupled from the backend and communicates via APIs only.

## Consequences
Positive:
- Vite provides extremely fast startup and hot module replacement, ideal for rapid demo iteration
- Tailwind’s utility-first approach yields high development speed and consistent styling
- TypeScript + Redux is well-suited to complex state (multi-account, filtering, pagination, dashboard data)
- Mature ecosystem that is easy to extend later

Negative:
- Heavier than plain React + CSS Modules
- Additional learning required for Redux patterns and Tailwind conventions
- Front-end / back-end separation increases integration effort

## Alternatives considered
1. Next.js + Server Components — adds SSR complexity that the current demo does not need
2. Vue 3 + Pinia + Vite — the team prefers the React ecosystem
3. Create React App — superseded by Vite and has poorer performance
4. CSS Modules / Styled-components — lower development efficiency compared with Tailwind
