# Student Marks Management API (Assignment 1)

ASP.NET Core 8 + EF Core + PostgreSQL API for student marks, with JWT auth, Postman tests, and a startup smoke-test module.

## Demo users (development only)

| Username | Password | Role |
|----------|----------|------|
| teacher1 | Teacher@123 | Teacher |
| student1 | Student@123 | Student |
| student2 | Student@123 | Student |

## Requirements

- .NET 8 SDK
- PostgreSQL 16+
- Postman
- Git

## Setup

### 1. Database

```sql
CREATE USER marks_user WITH PASSWORD 'MarksPass123';
CREATE DATABASE marks_db OWNER marks_user;
```

### 2. Secrets (from `src/MarksApi`)

```powershell
cd src/MarksApi
dotnet user-secrets set "ConnectionStrings:Database" "Host=127.0.0.1;Port=5432;Database=marks_db;Username=marks_user;Password=MarksPass123"
dotnet user-secrets set "Jwt:Key" "MarksApiDevSecretKey_ChangeMe_32chars_min_OK"
dotnet user-secrets set "DemoUsers:TeacherPassword" "Teacher@123"
dotnet user-secrets set "DemoUsers:StudentPassword" "Student@123"
```

### 3. Migrate and run

```powershell
dotnet ef database update
dotnet run
```

API default: `http://localhost:5041`  
Health: `/health`  
Swagger: `/swagger`

### 4. Smoke test

```powershell
dotnet run -- --smoke-test
```

Exits 0 if all checks pass.

### 5. Postman

1. Import `postman/MarksApi.postman_collection.json`
2. Import `postman/Local.postman_environment.json`
3. Select environment **Local**
4. Confirm `baseUrl` is your API address
5. Run collection with **Collection Runner** (keep folder order)

## Main endpoints

- `POST /api/auth/login`
- Students CRUD + transcript
- Courses CRUD + stats
- Enrollments + mark update
- `GET /health`

## Design notes

- Unique conflicts return **409** (insert then handle DB unique violation)
- Validation issues return **400**
- Students may access only their own student/transcript data (**403** otherwise)
- Pass mark from config `Academic:PassMark`

## Repo layout

```text
src/MarksApi/
postman/
db/schema.sql
docs/erd.png
docs/report.md
docs/screenshots/
appsettings.Example.json
README.md
```

## Schema dump

```powershell
$env:PGPASSWORD='MarksPass123'
pg_dump -h 127.0.0.1 -U marks_user -d marks_db --schema-only -f db/schema.sql
```
