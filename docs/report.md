# Student Marks Management API — Assignment 1 Report

**GitHub repository:** https://github.com/falcon1354/22F-3161_Ahmad_Assignment1  
**Student:** Ahmad Zubair (22F-3161)  
**Course:** Advanced Programming (C# and .NET)

## 1. Architecture

The API is an ASP.NET Core 8 minimal API with clear layers:

- **Endpoints** (`Endpoints/ApiEndpoints.cs`): HTTP routing only
- **Services** (`Services/`): business rules behind interfaces
- **Data** (`Data/`): `AppDbContext`, entities, EF Core migrations, seeder
- **DTOs** (`Dtos/`): request/response models (entities are never returned)
- **SmokeTests**: optional end-to-end checks started with `--smoke-test`

PostgreSQL stores all data. JWT authentication protects endpoints. Teachers manage students/courses/marks; students may read only their own record and transcript.

## 2. ER Diagram

See `docs/erd.png`.

Tables:

- `students` — registration number and email unique
- `courses` — code unique; credit hours 1–4
- `enrollments` — unique `(student_id, course_id)`; mark null or 0–100; FK delete restricted
- `users` — hashed passwords; role Teacher/Student; optional `student_id`

## 3. Design decisions

### Why 409 instead of 400 for duplicates
A duplicate registration number, email, course code, or double enrollment is a **conflict with existing data**, not a malformed request. The body can be valid JSON with valid field types, but inserting it would break a uniqueness rule. HTTP 409 Conflict matches that meaning. Validation problems (empty name, mark 101) remain 400.

### Duplicates and concurrent requests
The API does **not** check-then-insert. It inserts and catches PostgreSQL unique violations (`DbUpdateException`), then returns 409. Unique indexes are the last line of defence if two requests arrive together.

### Pass mark from configuration
Pass mark is read from `Academic:PassMark` (default 50), not hard-coded in business formulas for “passed”.

### Weighted transcript average
Over graded courses only:  
`(sum(mark × credit_hours)) / (sum(credit_hours))`.  
Ungraded enrollments are ignored. If none are graded, average is null.

## 4. Security and production practices

- Passwords hashed with `PasswordHasher<User>`
- JWT key and DB password stored in user-secrets / environment variables
- Problem Details for errors; global handler hides stack traces
- Async EF calls with cancellation tokens
- `AsNoTracking` for reads; paging/search/sort on lists
- Health check at `/health`
- Swagger UI for documentation
- CORS limited to configured origins
- UTC timestamps only

## 5. Testing

- **Postman collection** `postman/MarksApi.postman_collection.json` with folders Auth, Students, Courses, Enrollments and Marks, Reports, Health, Negative Tests (≥30 requests, ≥10 negatives)
- **Smoke test:** `dotnet run -- --smoke-test` (real HTTP + real PostgreSQL, cleans SMOKE- data)

## 6. Problems faced and solutions

1. **`dotnet` not found in terminal** — SDK installed but PATH not refreshed; fixed by adding `C:\Program Files\dotnet` to user PATH and reopening terminal.
2. **EF design-time used a fake password** — `dotnet ef database update` failed until connection was passed explicitly / design-time factory read user-secrets.
3. **Port already in use** — second `dotnet run` failed because the first API was still bound to 5041; keep one instance only.
4. **psql password appears blank** — normal; characters are hidden while typing.

## 7. How to run (summary)

See root `README.md` for full steps: create DB, set user-secrets, `dotnet ef database update`, `dotnet run`, import Postman files, run collection + smoke test.
