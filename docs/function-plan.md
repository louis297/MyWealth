# MyWealth Function Plan (MVP)

**Version**: v0.1  
**Status**: Draft  
**Last Updated**: 2026-08-26  

---

## 1. Purpose

This document defines the **MVP feature scope** of MyWealth, a wealth management SaaS demo platform.  

It focuses on **what** the system should do, **who** can do it, and the **main pages/interactions**.  

Technical implementation details (Commands, Queries, API contracts, etc.) are deliberately excluded and will be covered in Domain Model, Architecture, API Design, or individual Feature Specs.

---

## 2. Roles and Permissions Overview

| Role            | Can Log In | Description |
|-----------------|------------|-------------|
| System Admin    | Yes        | Highest platform authority. Can manage all tenants. |
| Tenant Admin    | Yes        | Highest authority within a single tenant. Can manage Advisers and Customers of the tenant. |
| Adviser         | Yes        | Can manage Customers assigned to them, including their Accounts, Holdings, and Transactions. |
| Customer        | **No**     | Pure business entity. Cannot log in. Must be bound to one Adviser when created. |

> Note: A Customer must be assigned to an Adviser at creation time.

---

## 3. Feature Modules

### 3.1 Identity & Authorization

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| Login                      | Email + password login, returns JWT              | Login page                       | All login-capable roles             |
| Logout                     | Clear current session                            | Global navigation                | All login-capable roles             |
| Profile Management         | Update name and password (no avatar)             | Profile page                     | All login-capable roles             |
| Role-based Access Control  | Control menu visibility and data scope by role   | Global                           | Built-in                            |

---

### 3.2 Tenant Management

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| Tenant List                | Paginated list with IsEnabled filter and Id/Name search | API only (`/tenants`) — no MVP UI | System Admin                        |
| Create Tenant              | Create a firm. Does not create a TenantAdmin    | API only                         | System Admin                        |
| Edit Tenant                | Rename or enable/disable                         | API only                         | System Admin                        |
| Tenant Admin List          | Paginated list with IsEnabled / TenantId filters and Id/Name/Email search | API only (`/tenant-admins`) — no MVP UI | System Admin |
| Create Tenant Admin        | Create a TenantAdmin + login for an enabled Tenant | API only                         | System Admin                        |
| Edit Tenant Admin          | Rename or enable/disable                         | API only                         | System Admin                        |
| Disable Tenant Admin       | Soft-disable. Last admin of a Tenant is allowed  | API only                         | System Admin                        |

---

### 3.3 Tenant User Management

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| Adviser List               | View all Advisers under the current tenant       | Adviser list page                | Tenant Admin                        |
| Create Adviser             | Create a new Adviser account                     | Create Adviser form              | Tenant Admin                        |
| Edit Adviser               | Update Adviser information, enable/disable       | Edit Adviser form                | Tenant Admin                        |
| Delete Adviser             | Delete an Adviser (must handle assigned Customers) | Confirmation dialog            | Tenant Admin                        |

---

### 3.4 Customer Management

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| Customer List              | Search and pagination. Advisers only see their own Customers | Customer list page         | Tenant Admin, Adviser               |
| Customer Detail            | View basic information + related Accounts overview | Customer detail page           | Tenant Admin, Adviser               |
| Create Customer            | Create a Customer. **Must bind to an Adviser**   | Create Customer form             | Tenant Admin, Adviser               |
| Edit Customer              | Update Customer basic information                | Edit Customer form               | Tenant Admin, Adviser               |
| Delete Customer            | Delete a Customer and related data               | Confirmation dialog              | Tenant Admin, Adviser               |

---

### 3.5 Account Management

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| Account List               | View all Accounts of a Customer (filter by status / customerId) | Customer detail / Account list   | Tenant Admin, Adviser               |
| Create Account             | Create an Account with type and currency         | Create Account form              | Tenant Admin, Adviser               |
| Edit Account               | Update Account name and/or type (currency immutable) | Edit Account form            | Tenant Admin, Adviser               |
| Close Account              | Permanently close an Account (Status = Closed; no forced clear of Holdings/Transactions) | Confirmation dialog | Tenant Admin, Adviser               |
| Account Detail             | View basic Account info (Holdings/Transactions overview comes later) | Account detail page     | Tenant Admin, Adviser               |

**Account Type Enum (MVP)**:

| Value       | Description              | Notes                              |
|-------------|--------------------------|------------------------------------|
| Bank        | Bank account             | Current / savings accounts         |
| Cash        | Cash                     | Physical cash                      |
| Brokerage   | Brokerage account        | Stocks, funds, ETFs, etc.          |
| Property    | Property                 | Real estate valuation              |
| Credit      | Liability account        | Credit cards, loans (liabilities)  |
| Other       | Other                    | Fallback                           |

---

### 3.6 Holding Management

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| Holding List               | View all Holdings under an Account               | Inside Account detail page       | Tenant Admin, Adviser               |
| Create Holding             | Add a Holding (instrument, quantity, cost basis, etc.) | Create Holding form         | Tenant Admin, Adviser               |
| Edit Holding               | Update Holding information                       | Edit Holding form                | Tenant Admin, Adviser               |
| Delete Holding             | Delete a Holding                                 | Confirmation dialog              | Tenant Admin, Adviser               |

---

### 3.7 Transaction Management

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| Transaction List           | Filter by Account, date range, and type          | Transaction list / Account detail | Tenant Admin, Adviser             |
| Create Transaction         | Support Buy, Sell, Transfer In, Transfer Out, Dividend, Interest, etc. | Create Transaction form | Tenant Admin, Adviser          |
| Auto-update Holding        | Automatically adjust Holding quantity and cost basis after a transaction | System behavior            | Built-in                            |

> API shipped with the Transactions slice (`GET`/`POST /transactions`). Adviser Portal pages can follow later.

---

### 3.8 Dashboard & Net Worth

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| Net Worth Overview         | Display current Net Worth (Assets − Liabilities) | Dashboard home                   | Tenant Admin, Adviser               |
| Asset Allocation View      | Show allocation by Account type or asset class   | Dashboard home                   | Tenant Admin, Adviser               |
| Account Performance Overview | Simple view of recent changes and returns      | Dashboard home                   | Tenant Admin, Adviser               |

> API shipped with the Dashboard slice (`GET /dashboard/net-worth`, `GET /dashboard/allocation`). Adviser Portal home (`/`) consumes the global view of both endpoints. Account Performance Overview remains out of scope. Historical Net Worth snapshots are out of scope for MVP.

---

### 3.9 Audit Log

| Feature                    | Description                                      | Main Pages / Interactions       | Permission                          |
|----------------------------|--------------------------------------------------|----------------------------------|-------------------------------------|
| View Audit Log             | Record key actions (who, when, what)             | Audit log list page              | Tenant Admin (own tenant), System Admin (all) |

---

## 4. Explicitly Out of Scope for MVP (Future Versions)

- Avatar upload
- Customer login capability
- Financial Goals
- Report export (CSV / PDF)
- In-app notifications
- Historical Net Worth snapshots
- Advanced custom transaction categories
- Bulk transaction import
- Automatic multi-currency conversion (currently handled per Account currency)

---

## 5. Next Steps

1. Confirm this Function Plan.
2. Proceed to English Feature Specs, Domain Model, or Architecture documentation as needed.