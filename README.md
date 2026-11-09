# Student Marks Management API (Assignment 1)

ASP.NET Core 8 + EF Core + PostgreSQL API for student marks, with JWT auth, Postman tests, and a startup smoke-test module.

## Demo users (development only)

| Username | Password | Role |
|----------|----------|------|
| teacher1 | Teacher@123 | Teacher |
| student1 | Student@123 | Student |
| student2 | Student@123 | Student |

## Requirements

- .NET 8 SDK (`dotnet --version`)
- PostgreSQL 16+ (`psql --version`)
- Postman
- Git

If `dotnet` is not recognized in PowerShell after install, reopen the terminal, or use:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" ...
```

## Setup (clean machine)

### 1. Create the database

Open **SQL Shell (psql)**, log in as `postgres`, then run:

```sql
CREATE USER marks_user WITH PASSWORD 'MarksPass123';
CREATE DATABASE marks_db OWNER marks_user;
```

If the user already exists, you can reset the password with:

```sql
ALTER USER marks_user WITH PASSWORD 'MarksPass123';
```

### 2. Set secrets (required)

From the repo root:

```powershell
cd src/MarksApi
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Database" "Host=127.0.0.1;Port=5432;Database=marks_db;Username=marks_user;Password=MarksPass123"
dotnet user-secrets set "Jwt:Key" "MarksApiDevSecretKey_ChangeMe_32chars_min_OK"
dotnet user-secrets set "DemoUsers:TeacherPassword" "Teacher@123"
dotnet user-secrets set "DemoUsers:StudentPassword" "Student@123"
```

Use the same DB password you set in step 1.  
Never commit real secrets. See `appsettings.Example.json` for every setting with fake values.

### 3. Install EF tool (first time only), migrate, and run

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update
dotnet run
```

Notes:
- If `dotnet ef` is not found, reopen the terminal after installing the tool.
- The app also runs migrations automatically on startup, so `dotnet run` alone works after secrets are set.
- Console prints the URL, usually `http://localhost:5041` (check your console if different).

### 4. Verify

- Health: `http://localhost:5041/health` → should show `Healthy`
- Swagger: `http://localhost:5041/swagger`

Keep that terminal open while testing. Do not start a second `dotnet run` on the same port.

### 5. Smoke test

In another terminal:

```powershell
cd src/MarksApi
dotnet run -- --smoke-test
```

Expect `SUMMARY passed=... failed=0` and exit code 0.

### 6. Postman

1. Import `postman/MarksApi.postman_collection.json`
2. Import `postman/Local.postman_environment.json`
3. Select environment **Local**
4. Set `baseUrl` to your API URL (example `http://localhost:5041`)
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

## Schema dump (optional regenerate)

```powershell
$env:PGPASSWORD='MarksPass123'
pg_dump -h 127.0.0.1 -U marks_user -d marks_db --schema-only -f db/schema.sql
```
