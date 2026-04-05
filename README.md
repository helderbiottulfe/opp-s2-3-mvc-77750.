# VGC College — Student & Course Management System

> ASP.NET Core 8 MVC application for Acme Global College (VGC) multi-branch student, course, and academic progress management.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Git

---

## Running the App Locally

```bash
# 1. Clone the repo
git clone https://github.com/<your-username>/oop-s2-1-mvc-<student-number>.git
cd oop-s2-1-mvc-<student-number>

# 2. Navigate to the web project
cd src/VgcCollege.Web

# 3. Apply migrations & seed the database (done automatically on first run)
dotnet run
```

The app will be available at **https://localhost:5001** (or http://localhost:5000).

The SQLite database (`vgccollege.db`) is created automatically in the project directory and seeded with demo data on first launch.

---

## Running Tests

```bash
# From the solution root
dotnet test --verbosity normal
```

Tests use an **EF Core InMemory** provider — no database setup required.

---

## Seeded Demo Accounts

All passwords follow the pattern shown below.

| Role    | Email                              | Password      |
|---------|------------------------------------|---------------|
| Admin   | admin@vgc.ie                       | Admin@123!    |
| Faculty | mary.smith@vgc.ie                  | Faculty@123!  |
| Faculty | john.doe@vgc.ie                    | Faculty@123!  |
| Student | alice.jones@student.vgc.ie         | Student@123!  |
| Student | bob.murphy@student.vgc.ie          | Student@123!  |
| Student | claire.ryan@student.vgc.ie         | Student@123!  |
| Student | david.kelly@student.vgc.ie         | Student@123!  |

---

## Seed Data Summary

- **3 branches**: VGC Dublin City, VGC Cork, VGC Galway
- **4 courses** across branches
- **2 faculty members** assigned to courses (Mary Smith is a tutor)
- **4 students** with enrolments, attendance, assignment and exam results
- **2 exams**: one with results released, one provisional (locked from students)

---

## Project Structure

```
VgcCollege/
├── .github/
│   └── workflows/
│       └── ci.yml                    # GitHub Actions CI
├── src/
│   └── VgcCollege.Web/
│       ├── Controllers/
│       │   ├── AdminController.cs    # Admin CRUD for all entities
│       │   ├── FacultyController.cs  # Faculty portal
│       │   ├── StudentController.cs  # Student portal
│       │   └── HomeController.cs
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   └── DbSeeder.cs
│       ├── Models/
│       │   └── Entities.cs           # All domain models
│       ├── Views/
│       │   ├── Admin/
│       │   ├── Faculty/
│       │   ├── Student/
│       │   ├── Home/
│       │   └── Shared/
│       ├── wwwroot/
│       ├── appsettings.json
│       └── Program.cs
└── tests/
    └── VgcCollege.Tests/
        └── CollectionTests.cs        # xUnit tests (12 tests)
```

---

## Design Decisions

### Authentication & Authorization
- ASP.NET Core Identity with **3 roles**: `Admin`, `Faculty`, `Student`
- All controllers use `[Authorize(Roles = "...")]` — server-side enforcement, not just UI hiding
- Faculty see only students enrolled in their assigned courses (DB-level filtering)
- Student contact details are restricted to faculty members with `IsTutor = true`
- Students **cannot** see provisional exam results — enforced via `ResultsReleased` boolean filter in the query

### Database
- SQLite with EF Core (easy local setup, no SQL Server required)
- Migrations run automatically on startup via `db.Database.MigrateAsync()`
- All seed data is idempotent (checks before inserting)

### Exam Result Visibility
- `Exam.ResultsReleased` is a boolean flag toggled by Admin only
- Student query always filters `&& r.Exam.ResultsReleased` — provisional results are completely invisible

### Testing
- 12 xUnit tests using EF Core InMemory provider
- Covers: enrolment rules, duplicate detection, exam visibility, grade validation, authorization-level query filtering, attendance calculation

---

## CI / CD

GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push/PR to `main`:
1. Restores NuGet packages
2. Builds in Release mode
3. Runs all xUnit tests — **fails the check if any test fails**
