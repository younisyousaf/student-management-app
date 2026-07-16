========================================================================
                       SCHOOL MANAGEMENT SYSTEM
========================================================================

A robust, highly scalable, and fully database-connected School Management 
Ecosystem built with modern C# and .NET practices. The project utilizes 
a Clean / Onion Architecture to separate concerns cleanly, housing both 
a high-performance C# Console Application for administrative tasks and 
a rich, responsive ASP.NET Core MVC Web Application with dynamic AJAX 
integrations.

------------------------------------------------------------------------
1. ARCHITECTURE OVERVIEW
------------------------------------------------------------------------

The system is designed using Onion / Clean Architecture principles to 
isolate business logic from databases and UI frameworks. This makes the 
codebase extremely testable, maintainable, and decoupled.

Directory Structure:

[Solution Folder]
 │
 ├── StudentManagement.Core               
 │    │-- Models/             (Domain Entities: Student, Course, etc.)
 │    └-- Interfaces/         (Repository and Service Interfaces)
 │
 ├── StudentManagement.Infrastructure     
 │    │-- EntityFramework/    (DbContext, Fluent API mappings)
 │    │-- Migrations/         (EF Core Database Schema Migrations)
 │    └-- Repositories/       (Generic & Specialized Repositories)
 │
 ├── StudentManagementApp.Console         
 │    └-- Program.cs          (CLI workflow for school administration)
 │
 └── StudentManagementApp.MVC             
      │-- Controllers/        (MVC Controllers handling UI logic)
      │-- ViewModels/         (UI-specific Data Transfer Objects)
      └-- Views/              (Razor Views with Bootstrap & Select2)


------------------------------------------------------------------------
2. TECHNOLOGY STACK & CONCEPTS
------------------------------------------------------------------------

BACKEND & DATABASE:
* C# (.NET 10)
* Entity Framework Core (EF Core): Applied using the Repository Pattern 
  with a decoupled Generic Repository context (EfRepository<T>).
* Fluent API: Native EF Core configurations mapped outside of core 
  entities to enforce database constraints (such as nullability and 
  precise decimal formatting for fees).
* SQL Server: Relational database backing both applications.

FRONTEND & UX (MVC):
* Bootstrap 5: Clean, modern styling with dynamic, color-coded badges 
  for student enrollment statuses.
* Select2 with AJAX: "Search-as-you-type" dropdowns. Instead of pulling 
  thousands of records, the UI queries dynamic database endpoints to 
  keep page loads instantaneous.
* jQuery & Validation: Clean script organization to prevent UI runtime 
  errors and secure front-end validation.


------------------------------------------------------------------------
3. KEY ARCHITECTURAL PATTERNS
------------------------------------------------------------------------

* The Repository Pattern: Separates business logic from raw SQL syntax.
* Post-Redirect-Get (PRG) Pattern: Prevents duplicate form submissions 
  (like double-charging fees or duplicate enrollments) on page refresh.
* Optimized In-Memory Joins: Utilizes fast C# Dictionary structures to 
  prevent the classic N+1 Query bottleneck.
* Deferred Execution & Pagination: Employs safe server-side query filters 
  using LINQ's .Skip() and .Take() to keep memory usage extremely low.


------------------------------------------------------------------------
4. GETTING STARTED
------------------------------------------------------------------------

PREREQUISITES:
* .NET 10 SDK
* SQL Server (LocalDB or Express)

DATABASE SETUP:
1. Update your database connection string in 'appsettings.json' inside the 
   StudentManagementApp.MVC directory.
2. Open your terminal in the StudentManagement.Infrastructure folder 
   and run:
   
   dotnet ef database update --startup-project ../StudentManagementApp.MVC

RUNNING THE MVC WEB APP:
1. Navigate to the web folder: cd StudentManagementApp.MVC
2. Run the project: dotnet run

RUNNING THE CONSOLE APP:
1. Navigate to the console folder: cd StudentManagementApp.Console
2. Run the project: dotnet run

========================================================================
