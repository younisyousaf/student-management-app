# Student Management System

A multi-client **Student Management System** built with **.NET, SQL Server, Angular, WPF, ASP.NET Core MVC, and ASP.NET Core Web API**, extended with an AI-powered **Student Management Copilot** using **Microsoft Agent Framework**.

The project demonstrates traditional application architecture together with AI tool calling, Human-in-the-Loop approval, persistent agent sessions, Retrieval-Augmented Generation (RAG), Qdrant, Agent Skills, and observability.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Projects](#projects)
- [Technology Stack](#technology-stack)
- [Data Access Strategy](#data-access-strategy)
- [Authentication](#authentication)
- [AI Copilot](#ai-copilot)
- [Human-in-the-Loop](#human-in-the-loop)
- [Persistent Sessions](#persistent-sessions)
- [RAG and Qdrant](#rag-and-qdrant)
- [Agent Skills](#agent-skills)
- [Observability](#observability)
- [Prerequisites](#prerequisites)
- [SQL Server Setup](#sql-server-setup)
- [Docker and Qdrant Setup](#docker-and-qdrant-setup)
- [Configuration](#configuration)
- [Running the Project](#running-the-project)
- [Testing the Copilot](#testing-the-copilot)
- [Project Status](#project-status)

---

## Overview

The solution was created to learn and demonstrate multiple .NET application types and data-access approaches while sharing the same Student Management domain.

It currently includes:

- Console application
- ASP.NET Core MVC application
- ASP.NET Core Web API
- WPF desktop client
- Angular standalone client
- SQL Server
- ADO.NET
- Entity Framework Core
- Dapper
- Stored Procedures
- JWT authentication
- Microsoft Agent Framework
- OpenRouter LLM integration
- AI tools
- Human-in-the-Loop approval
- SQL-backed agent sessions
- RAG over institutional documents
- Qdrant vector database
- Local embeddings
- Agent Skills
- AI/tool/RAG/request timing

The existing application services remain the authoritative source for operational data. The AI layer calls those services through controlled tools instead of directly becoming another database-access layer.

---

## Features

### Student Management

- Student CRUD
- Search students by name
- Retrieve students by ID
- Retrieve students by roll number

### Course Management

- Course CRUD
- Retrieve course by ID
- Retrieve course by code
- List courses
- Update course details and pricing

### Enrollment Management

- Enroll students
- Retrieve student enrollments
- Retrieve enrollment by ID

### Attendance Management

- Record attendance
- Mark attendance for "today"
- Update attendance
- Retrieve attendance records
- Generate attendance summaries

Date-sensitive operations use an application-configured timezone rather than depending on the server machine's local date.

### Fee Management

- Retrieve fee records
- Retrieve fee statements
- Process payments
- Track amount due
- Track amount paid
- Track remaining balance
- Track payment status

### Authentication

- Registration and login
- JWT authentication
- Protected API endpoints
- Angular authentication
- WPF API authentication

### AI Copilot

The Copilot can use:

- live SQL-backed application data,
- institutional policies retrieved through RAG,
- controlled write tools,
- Human-in-the-Loop approval,
- persistent sessions,
- Agent Skills.

Example questions:

```text
Show me all courses.

Get the fee statement for student ID 1 in course ID 5.

Is student ID 1 eligible to sit the final examination based on attendance?

Review the fee status for student ID 1 in course ID 5 and tell me whether institutional policy indicates any eligibility issue.
```

---

## Architecture

```text
                 Angular / WPF / MVC
                         |
                         v
                ASP.NET Core Web API
                         |
              +----------+----------+
              |                     |
              v                     v
      Application Services   StudentManagement.AI
              |                     |
              v                     v
          SQL Server       Microsoft Agent Framework
                                    |
                          +---------+---------+
                          |         |         |
                          v         v         v
                       AI Tools   Skills     RAG
                          |                   |
                          v                   v
                  Application Services     Qdrant
                                              |
                                              v
                                  Institutional Knowledge
```

The AI layer sits on top of the existing application architecture. It does not replace the normal services or database layer.

---

## Projects

### `StudentManagement.Core`

Shared domain models, interfaces, and application/service abstractions.

### `StudentManagement.Infrastructure`

ADO.NET infrastructure used for direct SQL/data-access learning.

### `StudentManagement.Infrastructure.EntityFramework`

Entity Framework Core infrastructure used by the MVC path.

### `StudentManagementApp.WebApi`

ASP.NET Core Web API responsible for:

- REST endpoints
- authentication
- dependency injection
- Copilot endpoints
- agent-session persistence
- application configuration

Development endpoints:

```text
https://localhost:7202
http://localhost:5164
```

### WPF Client

Desktop client consuming the Web API.

### Angular Client

Angular standalone frontend consuming the Web API.

### MVC Client

ASP.NET Core MVC implementation.

### `StudentManagement.AI`

Contains the Copilot implementation.

Major areas include:

```text
StudentManagement.AI/
├── Agents/
├── Configuration/
├── Context/
├── Extensions/
├── Observability/
├── RAG/
├── Services/
├── Sessions/
├── Skills/
└── Tools/
```

Responsibilities include:

- agent creation,
- OpenRouter chat-client configuration,
- AI tools,
- approval-required tools,
- RAG,
- Agent Skills,
- authenticated-user context,
- sessions,
- observability.

---

## Technology Stack

| Area | Technology |
|---|---|
| Runtime | .NET |
| Backend | ASP.NET Core Web API |
| Web UI | ASP.NET Core MVC |
| Desktop | WPF |
| Frontend | Angular |
| Database | SQL Server |
| ORM | Entity Framework Core |
| Other Data Access | Dapper, ADO.NET, Stored Procedures |
| Authentication | JWT |
| AI Framework | Microsoft Agent Framework |
| AI Abstractions | Microsoft.Extensions.AI |
| LLM Provider | OpenRouter |
| Vector Database | Qdrant |
| Embeddings | Local embedding generator |
| Containers | Docker |
| API Testing | Swagger / OpenAPI |

---

## Data Access Strategy

The solution intentionally uses several approaches for learning.

| Application / Layer | Data Access |
|---|---|
| Console | ADO.NET |
| MVC | Entity Framework Core |
| WPF | Web API |
| Angular | Web API |
| Web API | EF Core + Dapper |
| AI Copilot | Existing application services through AI tools |
| Institutional RAG | Qdrant |

Live student, course, enrollment, attendance, and fee information must come from the application's service layer.

---

## Authentication

The Web API uses JWT authentication.

The Angular client can authenticate through the API, while the WPF application consumes protected endpoints through its API client.

The Copilot also receives authenticated-user context so AI requests remain associated with the current application user.

---

# AI Copilot

The Copilot uses Microsoft Agent Framework.

```text
User Prompt
    |
    v
CopilotController
    |
    v
CopilotService
    |
    v
AIAgent
    |
    v
LLM
    |
    +----> Application AI Tools ----> Services ----> SQL Server
    |
    +----> Agent Skills
    |
    +----> SearchInstitutionalKnowledge ----> Qdrant
```

The agent is instructed to:

- only state application facts it can verify,
- avoid inventing student or institutional data,
- never infer roll numbers from student IDs,
- never infer course codes from course IDs,
- gather all required evidence for multi-part questions,
- separate live data from institutional policy,
- validate exact records before writes,
- check `Success` and `Found` values,
- avoid policy conclusions unsupported by retrieved policy,
- use authoritative calculated values returned by tools.

---

## OpenRouter

The project uses OpenRouter through its OpenAI-compatible API.

Example:

```json
{
  "OpenRouter": {
    "BaseUrl": "https://openrouter.ai/api/v1",
    "Model": "openrouter/free",
    "TimeoutSeconds": 85
  }
}
```

Never commit the OpenRouter API key to source control.

Use User Secrets, environment variables, or another secure secret store.

---

## AI Tools

Examples of tools exposed to the agent include:

### Student

```text
GetStudentById
GetStudentByRollNumber
SearchStudentsByName
```

### Course

```text
GetCourseById
GetCourseByCode
GetAllCourses
```

### Enrollment

```text
GetEnrollmentsByStudent
GetEnrollmentById
EnrollStudent
```

### Attendance

```text
GetAttendanceForStudent
GetAttendanceForCourseOnDate
GetAttendanceById
GetAttendanceSummaryForStudent
MarkAttendanceToday
UpdateAttendance
```

### Fees

```text
GetFeeById
GetFeeStatement
ProcessStudentPayment
```

### RAG

```text
SearchInstitutionalKnowledge
```

Write operations are protected by approval-required functions where appropriate.

---

# Human-in-the-Loop

Sensitive write operations use `ApprovalRequiredAIFunction`.

```text
User requests modification
        |
        v
Agent validates target
        |
        v
Agent requests approval-required tool
        |
        v
Execution pauses
        |
        v
API returns approval request
        |
        v
User approves/rejects
        |
        v
Persisted agent session resumes
        |
        v
Tool executes only when approved
```

This is used for operations such as:

- student enrollment,
- attendance writes,
- payment processing,
- student updates/deletion,
- course updates/deletion.

The agent does not ask for an extra conversational confirmation when application-level approval will handle confirmation.

---

# Persistent Sessions

Agent sessions are persisted in SQL Server.

`AgentSessions` stores information including:

- session ID,
- user ID,
- serialized session,
- creation/update timestamps,
- expiration,
- pending approval request ID,
- pending function name,
- pending function call ID,
- pending arguments.

Flow:

```text
HTTP Request
    |
    v
Load persisted session
    |
    v
Run / resume agent
    |
    v
Handle tools / approval
    |
    v
Serialize session
    |
    v
Save session to SQL Server
```

This allows conversations and Human-in-the-Loop workflows to continue across HTTP requests.

---

# RAG and Qdrant

Institutional policies are retrieved using Retrieval-Augmented Generation.

The system deliberately separates live data from policy knowledge.

```text
Live application data:
SQL Server -> Services -> AI Tools

Institutional knowledge:
Documents -> Chunking/Ingestion -> Embeddings -> Qdrant
                                         |
                                         v
                              SearchInstitutionalKnowledge
```

For example, attendance eligibility can require:

1. live attendance from SQL-backed tools,
2. attendance policy from Qdrant,
3. an evidence-based conclusion using both.

The agent must not invent policies or infer consequences that the retrieved policy does not explicitly establish.

---

## Qdrant Knowledge Store

`QdrantKnowledgeStore` performs vector retrieval.

Its query flow is:

```text
Initialize local embedding generator
        |
        v
Generate query embedding
        |
        v
Query Qdrant
        |
        v
Filter and map results
        |
        v
Return institutional knowledge
```

The RAG pipeline can retrieve chunks from institutional material such as the student handbook.

---

# Agent Skills

The Copilot supports Agent Skills for reusable task-specific workflows.

Current examples include:

```text
attendance-eligibility
fee-status-review
```

Example runtime logs:

```text
Agent requested skill loading. Arguments: [skillName, attendance-eligibility]
```

```text
Agent requested skill loading. Arguments: [skillName, fee-status-review]
```

Skills are useful for workflows that require a repeatable sequence of tools and evidence checks without placing every task-specific instruction in the global agent prompt.

---

# Observability

The project contains an `Observability` layer for measuring AI latency.

### `TimedChatClient`

Wraps `IChatClient` and logs LLM duration:

```text
LLM call finished in 3446 ms.
```

### `TimedAIFunction`

Wraps selected tools:

```text
AI tool GetAttendanceSummaryForStudent finished in 32 ms.
AI tool SearchInstitutionalKnowledge finished in 7691 ms.
```

### RAG timing

`QdrantKnowledgeStore` logs stages such as:

```text
Local embedding generator initialized in ... ms.
Query embedding generated in ... ms.
Qdrant query returned ... results in ... ms.
Qdrant results filtered and mapped in ... ms.
Knowledge search finished in ... ms.
```

### Agent timing

```text
Agent execution finished after ... ms.
```

### HTTP timing

```text
Copilot HTTP request finished after ... ms.
```

This helps identify whether latency comes from the LLM, tools, embedding generation, Qdrant, orchestration, or HTTP/database work.

---

# Prerequisites

Install:

- .NET SDK required by the solution
- SQL Server or SQL Server Express
- Visual Studio or another .NET IDE
- Node.js
- Angular CLI compatible with the project
- Docker Desktop
- Git

AI features additionally require:

- OpenRouter API key
- Qdrant
- institutional documents indexed into Qdrant

---

# SQL Server Setup

Development uses SQL Server Express:

```text
localhost\SQLEXPRESS
```

with Windows Authentication.

Example connection string:

```json
{
  "ConnectionStrings": {
    "SchoolDB": "Server=localhost\\SQLEXPRESS;Database=SchoolDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Change this configuration for your own SQL Server environment.

Apply the required database setup/migrations before starting the application.

---

# Docker and Qdrant Setup

Qdrant runs locally in Docker.

## 1. Verify Docker

Start Docker Desktop, then run:

```powershell
docker --version
docker ps
```

## 2. Pull Qdrant

```powershell
docker pull qdrant/qdrant
```

## 3. Run Qdrant

PowerShell:

```powershell
docker run -d `
  --name student-management-qdrant `
  -p 6333:6333 `
  -p 6334:6334 `
  -v qdrant_storage:/qdrant/storage `
  qdrant/qdrant
```

| Port | Purpose |
|---|---|
| `6333` | HTTP/REST API and dashboard |
| `6334` | gRPC |

The named `qdrant_storage` volume persists vectors across container recreation.

## 4. Verify

```powershell
docker ps
```

Qdrant REST endpoint:

```text
http://localhost:6333
```

Qdrant dashboard:

```text
http://localhost:6333/dashboard
```

## 5. Stop Qdrant

```powershell
docker stop student-management-qdrant
```

## 6. Start Qdrant again

```powershell
docker start student-management-qdrant
```

## 7. Recreate the container

```powershell
docker stop student-management-qdrant
docker rm student-management-qdrant
```

The named volume remains unless explicitly deleted.

To delete Qdrant's persisted vector data:

```powershell
docker volume rm qdrant_storage
```

> **Warning:** deleting the volume removes the indexed vectors. Institutional documents must then be indexed again.

---

## Optional Docker Compose

Create `docker-compose.yml` at repository root:

```yaml
services:
  qdrant:
    image: qdrant/qdrant
    container_name: student-management-qdrant
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - qdrant_storage:/qdrant/storage
    restart: unless-stopped

volumes:
  qdrant_storage:
```

Start:

```powershell
docker compose up -d
```

Check:

```powershell
docker compose ps
```

Stop:

```powershell
docker compose down
```

Delete the container and volume:

```powershell
docker compose down -v
```

---

# Configuration

## OpenRouter API Key

Example User Secrets setup from the Web API project:

```powershell
dotnet user-secrets init
dotnet user-secrets set "OpenRouter:ApiKey" "YOUR_OPENROUTER_API_KEY"
```

Do not commit real secrets.

## Application Time Zone

Example:

```json
{
  "Application": {
    "TimeZoneId": "Pakistan Standard Time"
  }
}
```

This is used for date-sensitive operations such as "mark attendance today".

## Qdrant

For local Docker development, Qdrant is normally available at:

```text
http://localhost:6333
```

Use the exact Qdrant option/property names already defined in the project configuration.

The collection's embedding dimensions/configuration must match the embedding model used during both ingestion and querying.

---

# Running the Project

## 1. Clone

```powershell
git clone <repository-url>
cd StudentManagementSystem
```

## 2. Restore

```powershell
dotnet restore
```

## 3. Start SQL Server

Make sure the configured SQL Server instance and application database are available.

## 4. Start Qdrant

```powershell
docker start student-management-qdrant
```

or:

```powershell
docker compose up -d
```

## 5. Configure OpenRouter

Add the API key through User Secrets or environment variables.

## 6. Prepare RAG data

Make sure the institutional documents have been ingested into the configured Qdrant collection.

If the Qdrant volume was deleted, run the project's ingestion/indexing process again before testing policy questions.

## 7. Build

From repository root:

```powershell
dotnet build
```

## 8. Run Web API

```powershell
dotnet run --project StudentManagementApp.WebApi
```

Development URLs:

```text
https://localhost:7202
http://localhost:5164
```

## 9. Run Angular

From the Angular project:

```powershell
npm install
ng serve
```

## 10. Run WPF / MVC

Select the required startup project in Visual Studio and run it normally.

---

# Testing the Copilot

A typical Copilot request:

```json
{
  "message": "Is student ID 1 eligible to sit the final examination based on attendance?",
  "sessionId": null
}
```

Typical response shape:

```json
{
  "response": "...",
  "sessionId": "...",
  "requiresApproval": false,
  "approval": null
}
```

Send the returned `sessionId` with later messages to continue the same conversation.

For write operations, `requiresApproval` can become `true`. The approval endpoint then resumes the persisted session after the user approves or rejects the operation.

---

## Attendance Eligibility Workflow

Conceptually:

```text
Load attendance-eligibility skill
        |
        v
GetStudentById
        |
        v
GetAttendanceSummaryForStudent
        |
        v
SearchInstitutionalKnowledge
        |
        v
Combine authoritative attendance data
with retrieved institutional policy
        |
        v
Evidence-based eligibility result
```

---

## Fee Status Review Workflow

Conceptually:

```text
Load fee-status-review skill
        |
        v
GetFeeStatement
        |
        v
SearchInstitutionalKnowledge
        |
        v
Report fee status
        |
        v
Only claim eligibility consequences
explicitly established by retrieved policy
```

An outstanding balance must not automatically be converted into an exam restriction when the retrieved policy does not establish that rule.

---

# Design Principles

1. SQL-backed application tools are authoritative for live operational data.
2. Qdrant/RAG is for institutional knowledge, not live student records.
3. The LLM does not directly modify the database.
4. Write operations go through application services and Human-in-the-Loop approval.
5. Exact target records are validated before writes.
6. IDs, roll numbers, and course codes are not inferred from one another.
7. Policy conclusions are grounded in retrieved institutional knowledge.
8. Missing evidence is reported instead of invented.
9. Calculated values returned by authoritative tools are used directly.
10. Agent Skills hold reusable task workflows.
11. Global agent instructions enforce cross-cutting safety/correctness rules.
12. Observability measures LLM, tool, RAG, agent, and HTTP latency.
13. SQL-backed sessions allow conversations and approval workflows to resume across requests.

---

# Project Status

Implemented:

- Student CRUD
- Course CRUD
- Enrollment management
- Attendance management
- Fee management
- JWT authentication
- Console application
- MVC application
- WPF client
- Angular client
- ASP.NET Core Web API
- Microsoft Agent Framework Copilot
- OpenRouter integration
- AI tool calling
- Human-in-the-Loop approval
- SQL-backed agent sessions
- RAG
- Qdrant
- Local embeddings
- Institutional knowledge search
- Combined live-data + policy reasoning
- Agent Skills
- AI/tool/RAG/HTTP observability

The project is primarily a learning and architecture project demonstrating how traditional .NET applications can be extended with an AI agent while keeping live data, authorization, write safety, and institutional policy grounded in controlled application components.
