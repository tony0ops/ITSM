# ITSM Ticket Management System

**A web-based service desk application built with ASP.NET Core 8, Razor Pages, Entity Framework Core, and SQLite.**

![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-5C2D91)
![EF Core 8](https://img.shields.io/badge/Entity%20Framework-Core%208-6B4FBB)
![SQLite](https://img.shields.io/badge/Database-SQLite-003B57?logo=sqlite&logoColor=white)
![Project](https://img.shields.io/badge/Project-Coursework-64748B)

This project was developed as part of a course project to bring **support tickets, customer records, assignments, conversations, SLA tracking, and working time** into one application.

**Managers and Agents** work in the internal service desk. **Customers** use a separate portal to follow their company's tickets, submit requests, and reply when permitted.

---

## Contents

- [Features](#features)
- [Technology Stack](#technology-stack)
- [Roles and Permissions](#roles-and-permissions)
- [Application Design](#application-design)
- [Getting Started](#getting-started)
- [Ticket Workflow](#ticket-workflow)
- [Data and Attachments](#data-and-attachments)
- [Limitations and Roadmap](#limitations-and-roadmap)
- [Project Documentation](#project-documentation)

## Features

| Area | What's included |
| --- | --- |
| **Ticket management** | Create, view, edit, search, filter, sort, delete, and restore tickets; paginated lists with 10, 25, 50, or 100 items per page. |
| **Queues and assignment** | Route tickets to assignment groups and users, view personal and team queues, and claim eligible unassigned tickets. |
| **Conversations** | Public replies, internal notes, reply attachments, and activity history. |
| **Time tracking** | Record work on tickets and view customer hours and cost summaries. |
| **SLA monitoring** | Deadlines, colour indicators, customer-specific targets, and pauses while waiting for a customer. |
| **Customer portal** | Company-level ticket visibility, customer replies, and new ticket requests controlled by individual permissions. |
| **Administration** | Manage customers, support teams, Manager/Agent accounts, portal accounts, and customer SLA settings. |
| **Dashboard** | Ticket counts, SLA indicators, and customer-filtered summaries. |

## Technology Stack

| Layer | Technology |
| --- | --- |
| Language and runtime | C# / .NET 8 |
| Web framework | ASP.NET Core 8 with Razor Pages |
| Data access | Entity Framework Core 8 |
| Database | SQLite |
| Authentication | ASP.NET Core Identity |
| Front end | Bootstrap, jQuery, and JavaScript |
| Version control | Git |

## Roles and Permissions

| Role | Main capabilities |
| --- | --- |
| **Manager** | Work with tickets; manage customers, SLAs, accounts, teams, deletion, and restoration. |
| **Agent** | Work with tickets, queues, assignments, replies, notes, attachments, time entries, and the dashboard. View customer and team lists. |
| **Customer** | Use the separate portal to access the linked company's tickets, according to permissions assigned by a Manager. |

The administrative role is called **Manager**; there is no separate **Admin** role.

Customer portal permissions are controlled through `CustomerUser`:

- **Can View Tickets** — view the linked company's tickets.
- **Can Add Comments** — reply to tickets when eligible.
- **Can Create Tickets** — submit new support requests; ticket viewing permission is also required.

Customers cannot access internal service desk pages, internal notes, or private files. Permissions are checked on the server, not just by hiding buttons in the interface. The company and its portal account link must both be active.

## Application Design

The project uses object-oriented models, strongly typed Razor PageModels, and shared services to separate data, page handling, and reusable logic.

### Main Entities

| Entity | Purpose |
| --- | --- |
| `Ticket` | Support request, status, severity, assignment, and deadlines. |
| `Customer` | Company information, contact details, and hourly rate. |
| `Team` | Support team and its assignment group. |
| `UserProfile` | Additional account information for a Manager or Agent. |
| `CustomerUser` | Links a portal account to a company and stores its permissions. |
| `CustomerSla` | Customer-specific time targets for different severities. |
| `TicketComment` | Public replies and internal notes. |
| `TicketActivity` | Ticket changes and recorded working time. |
| `TicketAttachment` | File details and links to tickets or replies. |

`TicketStatus`, `Severity`, `TicketCategory`, and `AssignmentGroup` are **enums** (fixed sets of values), not separate database entities. Entities inherit common IDs and timestamps from `BaseEntity`. Interfaces support activation and soft deletion.

### Layers

```text
Manager / Agent pages ──┐
                        ├── Razor Pages + PageModels ── Services / helpers
Customer portal ───────┘             │                         │
                                      └──── ApplicationDbContext ─── SQLite (app.db)
                                                                │
                                                  Private attachment storage
```

- **Data:** `ApplicationDbContext` defines relationships, query filters, and indexes. EF Core migrations track schema changes; SQLite stores application data and Identity accounts.
- **UI:** Razor Pages provide forms, lists, and navigation. PageModels process requests, check access, validate input, and coordinate database operations.
- **Shared logic:** Services and helpers handle attachment storage, selection lists, ticket formatting, SLA calculations, and activity messages.
- **Validation:** DataAnnotations define rules such as required fields and maximum lengths.

## Getting Started

### Requirements

Install either the **.NET 8 SDK**, or **Visual Studio** with the *ASP.NET and web development* workload and .NET 8 SDK.

### Run in Visual Studio

1. Open `WebApplication1.sln`.
2. Select the `http` launch profile and start the application.
3. If a browser does not open automatically, visit **http://localhost:5019** (the port configured for that profile).

### Run from the Command Line

From the directory containing `WebApplication1.csproj`:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile http
```

The supplied `appsettings.json` enables `LabMode:Enabled`. In Lab Mode, startup applies existing migrations and creates the database tables when needed. A fresh checkout starts without previous accounts, customers, or tickets.

> [!WARNING]
> **Lab Mode is for local coursework and demonstrations.** The special first-Manager setup and public offline password reset are not suitable for a publicly accessible deployment without redesign and security review.

### Create the First Manager Account

1. Register using the exact email **`test1@test.com`** and a password you choose.
2. Stop and restart the application.
3. Log out if necessary, then log back in with that account.
4. The account receives **Manager** access at startup, provided it is not linked to a customer company.

The lab account's role is assigned on startup, **not** immediately on registration or password reset. Other registered accounts require access assigned by a Manager. Real email delivery and email confirmation are not required for this lab workflow.

### Set Up Staff and Customer Accounts

1. Sign in as Manager, create a **Team**, and select its support assignment group.
2. Open **Users → Create Staff User** to create Manager or Agent accounts. Give Agents who need to claim tickets an active team.
3. Create a company in **Customers**.
4. Open the company's **Details** page and create or link a portal user. A company's contact email alone does not create a login.
5. Assign **Can View Tickets**, **Can Add Comments**, and **Can Create Tickets** as needed.
6. Sign in as the portal user to test the Customer experience.

An existing suitable login can be linked to a company without creating a new password. Accounts with Manager/Agent roles or an internal user profile cannot be linked as customer portal users.

## Ticket Workflow

### Internal Navigation

| Page | Purpose |
| --- | --- |
| **Dashboard** | Ticket totals, SLA indicators, and customer-filtered summaries. |
| **All Tickets** | Search, filter, sort, and open tickets; open tickets appear by default. |
| **Unassigned Tickets** | Review unassigned open tickets and claim eligible selections. |
| **My Tickets** | Tickets assigned to the signed-in staff member. |
| **Team Queue** | Tickets belonging to the staff member's team's assignment group. |
| **Customers** | Company information; Managers also configure SLAs and portal accounts. |
| **Users** | Manager-only account creation, roles, profiles, and team membership. |
| **Teams** | Team information and assignment groups; management requires Manager access. |

### Assignment Rules

To claim a ticket, a staff member needs an **active team with an assignment group**. The ticket must be open and unassigned, and must belong to that group or have no group yet. An unrouted ticket adopts the claiming person's team group. Claiming does not replace an existing assignee.

Managers and Agents can also edit assignments, but the selected person must be eligible for the chosen group.

### Customer Portal Rules

Customer-created tickets begin with **New** status and are linked automatically to the customer's company. Portal users can see company-level tickets, including tickets submitted by other contacts from the same company, when their permissions allow it.

A public customer reply resumes a ticket that is waiting on the customer and reopens a **Resolved** ticket as **In Progress**. **Closed** and **Cancelled** tickets remain readable, but further customer communication requires a new request.

## Data and Attachments

### Database

SQLite uses the connection string `Data Source=app.db`. The database stores tickets, customers, accounts, and attachment metadata.

To move an existing working dataset, back up **both**:

```text
app.db                   # Database and account records
App_Data/attachments/    # Private uploaded file contents
```

The database file is resolved relative to the application's working directory. Check that location if records appear to be missing after moving or launching the project from another folder.

### File Uploads

Attachments are stored **outside `wwwroot`**, and access is checked when a file is downloaded. New attachments appear with their replies; older ticket-level files remain in a separate list.

| Setting | Value |
| --- | --- |
| Maximum size | 10 MB per file |
| Supported formats | PDF, PNG, JPG/JPEG, GIF, WebP, TXT, LOG, CSV, DOC, DOCX, XLSX |
| Validation | File extension and basic file-content checks |
| Access | Checked before download |

Basic validation is not a substitute for malware scanning.

## Limitations and Roadmap

### Current Limitations

- **Local-first security setup:** Lab Mode and the offline password-reset shortcut require changes before public deployment.
- **SLA calendar:** Time includes nights and weekends unless paused; working hours and public holidays are not modeled.
- **Timezones:** Timestamps use the server's local timezone; moving data between timezones requires care.
- **Performance:** Some SLA views and summaries evaluate matching records before paging; larger datasets need additional testing.
- **Integrations:** Email notifications, malware scanning, and PDF invoice generation are not implemented.
- **Billing:** Cost summaries are simple *hours × hourly rate* calculations, not a complete billing system.

### Planned Improvements

- Email notifications for ticket creation, replies, and status changes.
- Reports of time spent by ticket, customer, and period, including PDF invoices.
- More dashboard charts and team/customer performance metrics, with appropriately scoped customer summaries.
- Business-hours and holiday calendars for SLA calculation.
- Additional automated tests and performance improvements for larger databases.
- Knowledge base, expanded attachment formats, and drag-and-drop uploads.

## Project Documentation

- [**PROJECT_STRUCTURE.md**](PROJECT_STRUCTURE.md) — project tree and explanations of major files.

Local databases, private attachments, build output, Visual Studio settings, generated coursework material, and old export snapshots are excluded from the source package. Third-party frontend libraries and their licence files are kept under `wwwroot/lib/`.

---

<sub>Course project · ASP.NET Core 8 · Razor Pages · Entity Framework Core · SQLite</sub>
