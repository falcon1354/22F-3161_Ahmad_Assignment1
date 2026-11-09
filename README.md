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

## Exact tested run steps (Windows)

These are the exact commands verified on this machine.

### What you’re doing

You’re turning the API “on” so Postman/browser can use it.  
Like starting an app, but this app also needs a database.

### Step 0 — Make sure PostgreSQL is running

1. Open Services on Windows (search “Services”).
2. Find something like `postgresql-x64-16`.
3. If it says Stopped, right-click → Start.

### Step 1 — Create the database (only once on this PC)

1. Open SQL Shell (psql).
2. Keep pressing Enter until it asks for password.
3. Type your postgres password (nothing will show) → Enter.
4. When you see `postgres=#`, paste these one by one:

```sql
CREATE USER marks_user WITH PASSWORD 'MarksPass123';
```

```sql
CREATE DATABASE marks_db OWNER marks_user;
```

```sql
\q
```

If it says user/database already exists, that’s fine. Continue.

### Step 2 — Open terminal in the API folder

In VS Code / PowerShell, run:

```powershell
cd "c:\Users\Falcon\Downloads\FAST\Semester 9\Advanced Programming\Assignment 1\Code\src\MarksApi"
```

### Step 3 — Save the secret settings (only once on this PC)

Copy-paste these 4 commands one by one:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" user-secrets set "ConnectionStrings:Database" "Host=127.0.0.1;Port=5432;Database=marks_db;Username=marks_user;Password=MarksPass123"
```

```powershell
& "C:\Program Files\dotnet\dotnet.exe" user-secrets set "Jwt:Key" "MarksApiDevSecretKey_ChangeMe_32chars_min_OK"
```

```powershell
& "C:\Program Files\dotnet\dotnet.exe" user-secrets set "DemoUsers:TeacherPassword" "Teacher@123"
```

```powershell
& "C:\Program Files\dotnet\dotnet.exe" user-secrets set "DemoUsers:StudentPassword" "Student@123"
```

This just tells the app: “use this database password and login password.”

### Step 4 — Start the project

Run:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run
```

Wait until you see something like:

```text
Now listening on: http://localhost:5041
Application started.
```

Leave this window open. Don’t close it.

### Step 5 — Check it works

Open Chrome and go to:

http://localhost:5041/health

If it says Healthy, the project is running.

Also open:

http://localhost:5041/swagger

### Step 6 — Test with Postman

1. Keep the API running (Step 4 window still open).
2. Open Postman.
3. Select environment Local.
4. Make sure baseUrl is `http://localhost:5041`.
5. Run the Marks API collection.

### How to stop

Click the running terminal and press Ctrl + C.

### Next time you want to run again

You usually only need:

```powershell
cd "c:\Users\Falcon\Downloads\FAST\Semester 9\Advanced Programming\Assignment 1\Code\src\MarksApi"
& "C:\Program Files\dotnet\dotnet.exe" run
```

Then open `/health` again.
