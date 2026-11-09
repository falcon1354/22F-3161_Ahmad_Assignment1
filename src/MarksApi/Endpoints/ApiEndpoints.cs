using System.Security.Claims;
using MarksApi.Dtos;
using MarksApi.Helpers;
using MarksApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarksApi.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            LoginRequest request,
            IAuthService authService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw AppException.BadRequest(
                    "Validation failed.",
                    new Dictionary<string, string[]>
                    {
                        ["username"] = string.IsNullOrWhiteSpace(request.Username)
                            ? ["Username is required."]
                            : Array.Empty<string>(),
                        ["password"] = string.IsNullOrWhiteSpace(request.Password)
                            ? ["Password is required."]
                            : Array.Empty<string>()
                    }.Where(x => x.Value.Length > 0).ToDictionary(x => x.Key, x => x.Value));
            }

            var result = await authService.LoginAsync(request, cancellationToken);
            return Results.Ok(result);
        }).AllowAnonymous();

        var students = app.MapGroup("/api/students").RequireAuthorization();
        students.MapGet("/", async (
            ClaimsPrincipal user,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] string? sort,
            IStudentService studentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            return Results.Ok(await studentService.GetStudentsAsync(page, pageSize, search, sort, cancellationToken));
        });

        students.MapGet("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            IStudentService studentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureCanAccessStudent(user, id);
            return Results.Ok(await studentService.GetByIdAsync(id, cancellationToken));
        });

        students.MapPost("/", async (
            CreateStudentRequest request,
            ClaimsPrincipal user,
            IStudentService studentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            var created = await studentService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/students/{created.Id}", created);
        });

        students.MapPut("/{id:int}", async (
            int id,
            UpdateStudentRequest request,
            ClaimsPrincipal user,
            IStudentService studentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            return Results.Ok(await studentService.UpdateAsync(id, request, cancellationToken));
        });

        students.MapDelete("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            IStudentService studentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            await studentService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        students.MapGet("/{id:int}/transcript", async (
            int id,
            ClaimsPrincipal user,
            IStudentService studentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureCanAccessStudent(user, id);
            return Results.Ok(await studentService.GetTranscriptAsync(id, cancellationToken));
        });

        var courses = app.MapGroup("/api/courses").RequireAuthorization();
        courses.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] string? sort,
            ICourseService courseService,
            CancellationToken cancellationToken) =>
            Results.Ok(await courseService.GetCoursesAsync(page, pageSize, search, sort, cancellationToken)));

        courses.MapGet("/{id:int}", async (
            int id,
            ICourseService courseService,
            CancellationToken cancellationToken) =>
            Results.Ok(await courseService.GetByIdAsync(id, cancellationToken)));

        courses.MapPost("/", async (
            CreateCourseRequest request,
            ClaimsPrincipal user,
            ICourseService courseService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            var created = await courseService.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/courses/{created.Id}", created);
        });

        courses.MapPut("/{id:int}", async (
            int id,
            UpdateCourseRequest request,
            ClaimsPrincipal user,
            ICourseService courseService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            return Results.Ok(await courseService.UpdateAsync(id, request, cancellationToken));
        });

        courses.MapDelete("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            ICourseService courseService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            await courseService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        courses.MapPost("/{courseId:int}/enrollments", async (
            int courseId,
            CreateEnrollmentRequest request,
            ClaimsPrincipal user,
            IEnrollmentService enrollmentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            var created = await enrollmentService.EnrollAsync(courseId, request, cancellationToken);
            return Results.Created($"/api/enrollments/{created.Id}", created);
        });

        courses.MapGet("/{courseId:int}/enrollments", async (
            int courseId,
            ClaimsPrincipal user,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IEnrollmentService enrollmentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            return Results.Ok(await enrollmentService.GetByCourseAsync(courseId, page, pageSize, cancellationToken));
        });

        courses.MapGet("/{courseId:int}/stats", async (
            int courseId,
            ClaimsPrincipal user,
            ICourseService courseService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            return Results.Ok(await courseService.GetStatsAsync(courseId, cancellationToken));
        });

        var enrollments = app.MapGroup("/api/enrollments").RequireAuthorization();
        enrollments.MapPut("/{id:int}/mark", async (
            int id,
            UpdateMarkRequest request,
            ClaimsPrincipal user,
            IEnrollmentService enrollmentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            return Results.Ok(await enrollmentService.UpdateMarkAsync(id, request, cancellationToken));
        });

        enrollments.MapDelete("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            IEnrollmentService enrollmentService,
            CancellationToken cancellationToken) =>
        {
            CurrentUser.EnsureTeacher(user);
            await enrollmentService.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
