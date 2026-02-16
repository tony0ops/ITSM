# ITSM Ticket Management System (ASP.NET Core)

Project is a web-based IT Service Management (ITSM) system developed using ASP.NET Core 8, Razor Pages, Entity Framework Core, and SQLite.

The application was developed as part of a course project.

## Technology Stack

- ASP.NET Core 8
- Razor Pages
- Entity Framework Core 8
- SQLite database
- ASP.NET Core Identity
- Bootstrap UI
- Git version control

## Object-Oriented Design

### Entities
- Ticket
- TicketStatus 
- Severity 

### Data Layer
- ApplicationDbContext
- Entity Framework Core migrations
- SQLite persistence

### UI Layer
- Razor Pages
- Strongly typed PageModels
- Model validation with DataAnnotations

## Current Status

- ✔ Identity configured  
- ✔ SQLite configured  
- ✔ Ticket CRUD functional  
- ✔ Filtering implemented  
- ✔ Git repository initialized  

## Planned Improvements

- SLA breach color indicators  
- Ticket comments (work notes)  
- Role-based access (Admin / Agent / Customer)  
- Service layer abstraction  
- Dashboard with statistics  
