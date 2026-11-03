# WebApplication1 - ITSM / service desk course project

A helpdesk application built with C#, .NET 8, ASP.NET Core Razor Pages, Entity
Framework Core 8, ASP.NET Identity, Bootstrap, and SQLite. It manages internal
support tickets for customer companies and provides a separate customer portal.

## Roles

| Role | Access |
| --- | --- |
| Manager | Tickets, ticket deletion, customers, SLAs, portal accounts, teams, staff profiles/roles, and dashboard. |
| Agent | Ticket creation/editing, comments, notes, time logging, queues, dashboard, and read-only customer/team lists. |
| Customer | Only the linked company's tickets, subject to active company/link and viewing/commenting permissions. |

Internal folders require the InternalStaff policy (Manager or Agent). Management
pages additionally require Manager. Portal handlers check company/link activity,
ticket ownership, and permissions on each request. Internal notes are hidden.

## Entities and object-oriented design

| Entity | Purpose and example attributes |
| --- | --- |
| Ticket | Number, title, description, status, severity, customer, assignment, SLA timestamps. |
| Customer | Company name, contact details, hourly rate, active/deleted state. |
| Team | Name, description, assignment group, active state. |
| UserProfile | Identity user link, display name, department, team, active state. |
| CustomerUser | Identity user/customer link, viewing/commenting permissions, active state. |
| CustomerSla | Customer-specific durations by severity, active state, notes. |
| TicketComment | Author, text, timestamps, internal/customer-visible flags. |
| TicketActivity | Type, author, old/new values, work description, time spent. |
| TicketAttachment | Ticket link, original/storage names, content type, size, uploader, and customer visibility. |

Identity supplies users and roles. Domain entities inherit BaseEntity (Id,
CreatedAt, UpdatedAt). IActivatable and ISoftDeletable describe lifecycle operations;
EntityLifecycleService demonstrates generic operations over those interfaces.
Activity formatters implement ITicketActivityFormatter and are selected by a factory.
TicketHelper centralizes display calculations; SlaPolicy calculates SLA state.
TicketStatus, Severity, TicketCategory, and AssignmentGroup are enums, not entities.

## Features

- Create/store/display/edit tickets; Managers can soft-delete tickets.
- Search by ticket number/title/description/customer; filter status, severity,
  category, assignment group, and SLA state. Default ticket list shows open tickets.
- Sort columns including title, created date, severity, and last activity.
- Customer-visible comments, internal notes, author-owned comment/time editing,
  activity timeline, and customer-replied badges/counts.
- Public customer replies reopen Resolved tickets and move WaitingOnCustomer tickets
  back to InProgress, recording status history and resuming any paused SLA. Closed or
  Cancelled tickets require a new ticket for further customer communication.
- Time logging and customer hours/revenue summaries (hours times hourly rate).
- Severity SLA defaults, customer overrides, manual deadlines, and pause/resume
  for WaitingOnCustomer and WaitingOnVendor.
- Personal, team, and unassigned queues; dashboard counts, resolution averages,
  and an SLA metric based on currently open tickets that are not breached.
- Customer create/edit/deactivate; SLA management; portal account create/link,
  activate/deactivate, permission management, and unlink. Portal users with both
  Can View Tickets and Can Create Tickets can submit tickets for their company.
- Portal ticket submissions start as New, receive an SLA deadline, and are linked
  to the signed-in user's active customer account; customer and assignment fields
  are set by the server.
- Team create/edit/deactivate/delete and existing staff profile/role/team management.
- Ticket attachments: staff and portal users can attach a file directly to a reply.
  Internal-note files inherit the note's private visibility; public-reply files follow
  the comment and ticket's customer visibility rules. Older ticket-level files remain
  listed separately. Downloads enforce ticket ownership and visibility. Each file is
  limited to 10 MB and accepted extensions and signatures are checked.
- Paginated ticket, personal/team/unassigned queue, customer, user, and team lists,
  plus the customer portal ticket list. Page sizes are 10, 25, 50, or 100, and current
  filters and sorting persist as you move between pages.

## Storage and structure

appsettings.json uses `Data Source=app.db`. SQLite stores application data and
Identity accounts in a local file, satisfying file-based storage requirements.
EF migrations create the schema. Ticket deletion sets IsDeleted and preserves
history; query filters hide deleted records. Customers have soft-delete model/service
support, while the customer UI exposes deactivation. Team deletion is permanent
and blocked while active members remain. Uploaded files are stored outside `wwwroot`
under `App_Data/attachments`; SQLite stores their metadata. Back up that folder together
with app.db. The application download handlers set `Content-Disposition: attachment`
and `X-Content-Type-Options: nosniff`.

- Models: entities, enums, interfaces, ticket/SLA helpers.
- Data: EF context, relationships, query filters, indexes, design-time factory.
- Migrations: database schema history.
- Pages: tickets, customers, teams, users, dashboard, and portal Razor Pages.
- Services: select lists, lifecycle operations, activity middleware/formatters.
- Areas/Identity/Pages/Account: accepted offline password recovery page.
- wwwroot: Bootstrap, JavaScript, CSS, and static assets.

The submission package excludes app.db, SQLite sidecars, build output, IDE state,
Git history, private `App_Data` uploads, and old code-export snapshots. Back up existing
databases and attachment files separately.

## How to run

Install the .NET 8 SDK. From the directory containing WebApplication1.csproj:

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile http
```

With the supplied `LabMode:Enabled` setting, startup applies existing migrations
before seeding roles, including for a new empty database. Open http://localhost:5019.
For HTTPS, trust the development certificate and use the https launch profile. Keep
your existing app.db when updating source files.

### Lab accounts and recovery

1. For a new empty database, register `test1@test.com` with a password of your choice,
   stop and restart the application, then log out and sign in again. With LabMode enabled,
   startup gives this existing account the Manager role, provided it is not linked to a customer.
   Public registration creates a sign-in account without staff privileges; a Manager
   can provision it as staff from Users or link it to a company from Customer Details.
   Email delivery/confirmation is not required.
2. Use Users → Create Staff User to create Manager or Agent accounts directly. Assign
   an active team so the Agent can work the Team Queue and claim eligible tickets.
   Manager is the administrative role; there is no separate Admin role.
3. Create a customer, then use Customer Details to create/link customer portal users.
   Portal users retain their Customer role and restricted company view.
4. Check Can View, Can Comment and Can Create as needed and click Save permissions
   for existing portal users. Older incorrectly saved permissions are not silently
   enabled by this update.
5. Offline password reset remains available. A Manager can list account
   names for recovery. Other accounts can be created and assigned access without restarting.

LabMode also repairs missing staff profiles on startup and preserves the existing
`test1@test.com` Manager bootstrap, unless that account is linked to a customer.
If LabMode is disabled, automatic migration/bootstrap is not performed; provision
staff deliberately. This configuration is intended for
an offline course demonstration.

### Navigation and assignment

Dashboard summarizes tickets; All Tickets defaults to open work and supports explicit
terminal-status filters. Unassigned Tickets contains all open tickets without a named
assignee. Staff with an active team can select multiple eligible rows in Unassigned
Tickets and claim them for themselves. Tickets routed to another group are not
claimable there; an unassigned-group ticket adopts the claiming agent's team group.
Teams must use a real support group; Unassigned is reserved for unrouted tickets.
My Tickets follows the signed-in assignee. Team Queue follows the assignment
group mapped to the user's team. Multiple teams may deliberately share a group and
therefore share its queue and assignee pool. Customers manages company configuration,
Users manages staff access, and Teams manages membership/group routing. Agents can
read customer/team lists; their management actions remain Manager-only.

## Accepted limitations

- Public offline password reset is intentionally retained for development/recovery.
- Portal ticket creation requires both CanViewTickets and CanCreateTickets.
  Managers can edit all three portal permissions from Customer Details. Attachments
  added by portal users require CanViewTickets and CanAddComments; portal users only
  see customer-visible attachments for tickets belonging to their linked company.
  Staff control customer visibility and Managers can delete any attachment (Agents can
  delete their own).
- Reply-linked files appear beneath their comment. Existing files uploaded directly to
  a ticket remain in the separate ticket-level attachment list and are not retroactively
  assigned to a historical reply.
- Lab registration creates a roleless account. A Manager provisions internal staff
  with Users → Create Staff User, or links an account to a company through Customer
  Details. Public registration never grants Manager access.
- Deleted tickets are soft-deleted and can be restored by a Manager from All Tickets →
  Deleted Tickets / Restore. Agents cannot restore or delete tickets.
- Account activity is checked on each request. Staff role edits invalidate existing
  sessions; affected users sign in again.
- Timestamps use server-local time. SLA durations use elapsed calendar time, not
  business hours. Keep the same server timezone when moving an existing database;
  the footer labels the display timezone. The dashboard SLA metric describes open-ticket state, not historical
  contractual compliance.
- Customer soft deletion has model/service support but no dedicated customer delete UI.
- Calculated SLA/last-activity ticket filtering scans matching scalar ticket rows
  before selecting a page so existing calculations stay correct.
  No outbound email notification service or malware scanner is included. Keep uploads
  on a trusted lab machine and review them before opening.
- Application diagnostics go to the console and Visual Studio Output window to avoid
  Windows Event Log write failures taking down a request under a limited local account.

See [TESTING.md](TESTING.md) for manual acceptance checks and
[PROJECT_STRUCTURE.md](PROJECT_STRUCTURE.md) for a guide to the source files.

The source repository excludes local databases, private uploads, IDE/build output,
generated coursework documents, code exports, and development review notes. A new
checkout starts without accounts or tickets; follow the lab setup above.
