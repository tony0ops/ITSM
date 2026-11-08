# Project files and responsibilities

WebApplication1 is an ASP.NET Core Razor Pages service desk application. The main
folders separate stored objects, database configuration, page actions, and shared
operations.

```text
WebApplication1/
|-- WebApplication1.sln          Visual Studio solution
|-- WebApplication1.csproj       .NET version and package references
|-- Program.cs                  Application setup, services, access rules, lab startup
|-- appsettings.json            SQLite connection, Lab Mode, attachment storage path
|-- appsettings.Development.json Development logging settings
|-- README.md                   Features, setup, roles, and limitations
|-- TESTING.md                  Manual checks for the application workflows
|-- PROJECT_STRUCTURE.md        This file guide
|-- .gitignore                  Files excluded from Git
|
|-- Models/
|   |-- Ticket.cs               Support request, status, severity, assignment, deadlines
|   |-- Customer.cs             Company, contacts, hourly rate, active state
|   |-- Team.cs                 Support team and assignment group
|   |-- UserProfile.cs          Extra information for a Manager or Agent
|   |-- CustomerUser.cs         Login-to-company link and portal permissions
|   |-- CustomerSla.cs          Customer-specific time targets
|   |-- TicketComment.cs        Public replies and internal notes
|   |-- TicketActivity.cs       Ticket changes and recorded working time
|   |-- TicketAttachment.cs     File details and connection to a ticket or reply
|   |-- BaseEntity.cs           Shared ID and timestamps
|   |-- TicketHelper.cs         Shared checks for ticket display
|   |-- SlaPolicy.cs            Deadline, pause, and SLA calculations
|   |-- TicketConversationFlow.cs Status changes caused by public replies
|   |-- PaginationViewModel.cs  Page sizes, page numbers, and selecting result pages
|   |-- Interfaces/             Active-state and soft-delete interfaces
|   `-- Other enum files       Status, severity, category, group, and activity choices
|
|-- Data/
|   |-- ApplicationDbContext.cs Tables, relationships, query filters, and indexes
|   `-- Other context files    Database setup for development tools
|-- Migrations/                 Recorded database changes and current model snapshot
|
|-- Pages/
|   |-- Index.cshtml            Home page
|   |-- AccessSetup.cshtml      Explains how an account receives application access
|   |-- Dashboard/             Ticket summaries and customer filtering
|   |-- Tickets/               Ticket lists, creation, editing, details, claiming, restore
|   |-- MyTickets/             Tickets assigned to the signed-in person and team views
|   |-- Customers/             Company records, SLAs, and portal-user management
|   |-- Users/                 Manager and Agent account creation and account management
|   |-- Teams/                 Support teams and their assignment groups
|   |-- Portal/                Customer ticket list, creation, replies, and downloads
|   `-- Shared/                Menu, page layout, login links, and reusable page controls
|
|-- Areas/Identity/Pages/Account/ Login, registration, account settings, password recovery
|-- Services/
|   |-- SelectListService.cs   Choices for customers, groups, and eligible assignees
|   |-- EntityLifecycleService.cs Shared activation and soft-delete operations
|   |-- InactiveUserMiddleware.cs Checks inactive accounts and customer links
|   |-- TicketAttachmentStorage.cs File validation and private file storage
|   `-- TicketActivity/        Readable messages and icons for activity entries
|
|-- wwwroot/
|   |-- css/                   Application styling
|   |-- js/                    Browser behaviour, including assignment choices
|   `-- lib/                   Existing frontend libraries and their licence files
`-- Properties/                Local launch profiles and service settings
```

## How a page connects to the code

A `.cshtml` file describes the page displayed in the browser. Its matching
`.cshtml.cs` file usually checks input and permissions, reads records, and saves
changes. The database context connects the model classes to SQLite. Services and
helpers contain operations reused by several pages.

For example, `Pages/Portal/CreateTicket.cshtml` displays the form, and
`Pages/Portal/CreateTicket.cshtml.cs` checks the customer account and saves a ticket
for that company. `Models/Ticket.cs` describes the saved ticket.

## Files created while running

- `app.db`: SQLite data, including accounts and tickets. Lab Mode applies migrations
  when the application starts, including for an empty database.
- `App_Data/attachments/`: private uploaded file contents. SQLite holds their details.
- `bin/` and `obj/`: build output and temporary build information.
- `.vs/` and `*.user`: local Visual Studio settings.

These files are not included in the source repository. To move an existing working
dataset, back up both `app.db` and `App_Data/attachments/` separately.
