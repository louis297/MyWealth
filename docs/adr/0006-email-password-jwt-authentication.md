# 0006. Authentication: Email/Password + JWT

## Status
Accepted

## Date
2026-08-18

## Context
As a SaaS wealth-management demo, MyWealth requires user registration and login. The solution should be simple, secure, friendly to a decoupled front-end/back-end architecture, and easy to extend later (e.g. social logins).

## Decision
Authentication will be implemented as follows:
- Users register and log in with email + password
- On successful validation the backend issues a JWT (JSON Web Token)
- The frontend stores the JWT (in memory or localStorage) and sends it in the Authorization header on subsequent requests
- ASP.NET Core Identity is used to manage users and password hashing (a lightweight custom implementation may be considered if complexity needs to be reduced)

## Consequences
Positive:
- Straightforward to implement for a demo
- JWT naturally supports a stateless, front-end/back-end separated architecture
- Password hashing follows Identity best practices
- Easy to extend later with refresh tokens, roles/permissions and third-party logins

Negative:
- Token expiry and refresh logic must be handled carefully
- Storing the token in localStorage carries an XSS risk (more secure storage should be used in production)
- Social logins (Google, Microsoft, etc.) are not supported in the first version

## Alternatives considered
1. Cookie + Session authentication — unsuitable for a fully decoupled front-end/back-end architecture
2. External Identity Provider (Auth0, Keycloak, etc.) — too heavy for a demo
3. Magic Link / passwordless login — more complex UX and implementation, deferred
4. Pure custom JWT without Identity — password management and security would have to be handled manually, higher risk
