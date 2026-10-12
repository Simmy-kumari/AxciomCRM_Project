# AcxiomCRM — Enterprise Customer Relationship Management System

## Project Overview
**AcxiomCRM** is a complete, role-based ASP.NET Core MVC web application for managing the customer-sales lifecycle, including lead capture, follow-ups, sales opportunity pipeline forecasting, role-based authorization, append-only security audit trails, and REST APIs.

---

## Technology Stack
- **Framework**: ASP.NET Core MVC (.NET 10.0)
- **Database**: SQLite with Entity Framework Core (`AcxiomCRM.db`)
- **Authentication & Security**: ASP.NET Core Identity (Argon2/PBKDF2 hashing, lockout policy, antiforgery tokens)
- **Frontend & UI**: Razor Views, Bootstrap 5.3, Bootstrap Icons, Chart.js, Custom SaaS Design System

---

## Default Roles & Test Credentials

The database comes pre-seeded with 4 active enterprise accounts across all 3 roles:

| Role | Email | Password | Scope & Privileges |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@acxiom.com` | `Admin@123!` | Full administration, user/role management, security audit log, all CRM records and analytics |
| **Sales Manager** | `manager@acxiom.com` | `Manager@123!` | Team-wide customer/lead/deal management, pipeline forecasts, management reports |
| **Sales Executive** | `sales@acxiom.com` | `Sales@123!` | Assigned customers, leads, pipeline opportunities, follow-up scheduling |
| **Sales Executive** | `emma@acxiom.com` | `Sales@123!` | Assigned customers, leads, pipeline opportunities, follow-up scheduling |

> **College Evaluation Feature**: Both the Login page and the top Navigation bar include a **1-Click Quick Demo Role Switcher** to quickly swap between Admin, Manager, and Sales Executive views without retyping passwords.

---

## Implemented Modules & Features

### 1. Dashboard (`/Dashboard`)
- **8 KPI Metrics**: Total Customers, Total Leads, Open Leads, Total Deals, Won Deals, Lost Deals, Pending Follow-Ups, and Active Pipeline Value ($).
- **4 Chart.js Visualizations**:
  - Lead Status Distribution (Doughnut)
  - Opportunity Pipeline Stage Breakdown (Bar)
  - Monthly Sales Performance (Line)
  - Pipeline Stage Value (Horizontal Bar)
- **Filters**: Today, This Week, This Month, All Time.
- **Widgets**: Recent Activity timeline, Upcoming Follow-Ups agenda, Quick Action menu.

### 2. Customer Management (`/Customers`)
- Master customer directory with search and status filters.
- Responsive table with Customer Code, Name, Company, Email, Phone, Status, Assigned Executive, and Actions.
- Customer Details page with tabbed sub-modules: Linked Deals, Scheduled Follow-Ups, Activity History, and Strategic Notes.
- Client-side and server-side validation ensuring valid email, phone, and duplicate checks.

### 3. Lead Management & Conversion (`/Leads`)
- Lead capture and tracking across statuses: `New`, `Contacted`, `Qualified`, `Unqualified`, `Converted`, `Lost`.
- Filter tabs by status, priority (`High`, `Medium`, `Low`), and lead source (`Website`, `Referral`, etc.).
- 4-step visual lead progression bar.
- **Lead Conversion Workflow (`/Leads/Convert/{id}`)**: Graduates qualified leads into Customer Accounts and creates associated Pipeline Deals.

### 4. Opportunity & Pipeline Management (`/Opportunities`)
- **Dual View**: Interactive **Kanban Board** (drag/stage-move menus) and structured **Table View**.
- Stages: `Qualification` (20%), `Proposal` (50%), `Negotiation` (80%), `Won` (100%), `Lost` (0%).
- Automatic calculation of **Weighted Pipeline Forecast** (`Amount × Probability / 100`).
- Validations: Amount must be > 0, Probability must be 0–100, Expected Close Date cannot be in the past for active deals.

### 5. Follow-Ups (`/FollowUps`)
- Scheduled touchpoints across Customers, Leads, and Opportunities.
- Filter tabs: `Upcoming`, `Pending`, `Completed`, `Missed / Overdue`.
- Validation rule: Follow-up date cannot be set in the past for planned activities.

### 6. Activity Management (`/Activities`)
- Chronological engagement logs for `Call`, `Meeting`, `Email`, and `Task`.
- Detailed minutes, meeting descriptions, and participant linkage.

### 7. Executive Reports (`/Reports`)
- 6 Analytical Reports:
  1. Sales Pipeline Report
  2. Lead Conversion Report
  3. Customer Portfolio Report
  4. Follow-Up Activity Report
  5. User Activity Report
  6. Audit Compliance Report
- Date range filtering, team member filtering, and print-ready layout.

### 8. User & Role Administration (`/Users`)
- Restricted to Admin and Manager roles.
- Create user, assign roles, activate/deactivate accounts, and view lockout states.

### 9. System Audit Logs (`/AuditLogs`)
- Append-only compliance log tracking logins, failed logins, role modifications, and CRM create/update/delete events.
- Filterable by User, Module, Action, and Date Range.

### 10. REST API Endpoints
Secured JSON API endpoints with DTO responses:
- `GET /api/customers`, `POST /api/customers`, `GET /api/customers/{id}`, `PUT /api/customers/{id}`, `DELETE /api/customers/{id}`
- `GET /api/leads`, `POST /api/leads`
- `GET /api/opportunities`, `POST /api/opportunities`
- `GET /api/followups`, `POST /api/followups`
- `GET /api/reports/pipeline`

---

## How to Run the Application

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)

### Run Command
In the project root directory:
```bash
dotnet run
```
Open your browser to:
- `http://localhost:5000` or `https://localhost:5001`
