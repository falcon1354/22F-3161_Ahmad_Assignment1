using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos;
using MarksApi.Helpers;
using MarksApi.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarksApi.Services;

public class CourseService(
    AppDbContext db,
    IOptions<AcademicOptions> academicOptions,
    ILogger<CourseService> logger) : ICourseService
{
    public async Task<PagedResult<CourseResponse>> GetCoursesAsync(
        int? page,
        int? pageSize,
        string? search,
        string? sort,
        CancellationToken cancellationToken)
    {
        var (p, size) = PagingHelper.Normalize(page, pageSize);
        var query = db.Courses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Code.ToLower().Contains(term) ||
                x.Title.ToLower().Contains(term));
        }

        query = (sort ?? "code").Trim().ToLowerInvariant() switch
        {
            "title" => query.OrderBy(x => x.Title),
            "credithours" or "credit_hours" => query.OrderBy(x => x.CreditHours),
            "id" => query.OrderBy(x => x.Id),
            _ => query.OrderBy(x => x.Code)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((p - 1) * size)
            .Take(size)
            .Select(x => new CourseResponse
            {
                Id = x.Id,
                Code = x.Code,
                Title = x.Title,
                CreditHours = x.CreditHours
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CourseResponse>
        {
            Items = items,
            Page = p,
            PageSize = size,
            TotalCount = total
        };
    }

    public async Task<CourseResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var course = await db.Courses.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new CourseResponse
            {
                Id = x.Id,
                Code = x.Code,
                Title = x.Title,
                CreditHours = x.CreditHours
            })
            .FirstOrDefaultAsync(cancellationToken);

        return course ?? throw AppException.NotFound($"Course {id} was not found.");
    }

    public async Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        ValidateCourse(request.Code, request.Title, request.CreditHours);

        var course = new Course
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Title = request.Title.Trim(),
            CreditHours = request.CreditHours,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Courses.Add(course);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbConflictHelper.IsUniqueViolation(ex))
        {
            throw AppException.Conflict("Duplicate course", "A course with this code already exists.");
        }

        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
        return ToResponse(course);
    }

    public async Task<CourseResponse> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        ValidateCourse(request.Code, request.Title, request.CreditHours);

        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw AppException.NotFound($"Course {id} was not found.");

        course.Code = request.Code.Trim().ToUpperInvariant();
        course.Title = request.Title.Trim();
        course.CreditHours = request.CreditHours;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbConflictHelper.IsUniqueViolation(ex))
        {
            throw AppException.Conflict("Duplicate course", "A course with this code already exists.");
        }

        logger.LogInformation("Updated course {CourseId}", course.Id);
        return ToResponse(course);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw AppException.NotFound($"Course {id} was not found.");

        var hasEnrollments = await db.Enrollments.AnyAsync(x => x.CourseId == id, cancellationToken);
        if (hasEnrollments)
        {
            throw AppException.Conflict(
                "Course has enrollments",
                "Cannot delete a course that still has enrollments.");
        }

        db.Courses.Remove(course);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted course {CourseId}", id);
    }

    public async Task<CourseStatsResponse> GetStatsAsync(int courseId, CancellationToken cancellationToken)
    {
        var passMark = academicOptions.Value.PassMark;
        var course = await db.Courses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == courseId, cancellationToken)
            ?? throw AppException.NotFound($"Course {courseId} was not found.");

        var marks = await db.Enrollments.AsNoTracking()
            .Where(x => x.CourseId == courseId)
            .Select(x => x.Mark)
            .ToListAsync(cancellationToken);

        var graded = marks.Where(x => x is not null).Select(x => x!.Value).ToList();

        return new CourseStatsResponse
        {
            CourseId = course.Id,
            CourseCode = course.Code,
            Enrolled = marks.Count,
            Graded = graded.Count,
            Average = graded.Count == 0 ? null : Math.Round(graded.Average(), 1),
            Highest = graded.Count == 0 ? null : graded.Max(),
            Lowest = graded.Count == 0 ? null : graded.Min(),
            Passed = graded.Count(x => x >= passMark)
        };
    }

    private static void ValidateCourse(string code, string title, int creditHours)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(code))
        {
            errors["code"] = ["Code is required."];
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Title is required."];
        }

        if (creditHours is < 1 or > 4)
        {
            errors["creditHours"] = ["Credit hours must be between 1 and 4."];
        }

        if (errors.Count > 0)
        {
            throw AppException.BadRequest("Validation failed.", errors);
        }
    }

    private static CourseResponse ToResponse(Course course) => new()
    {
        Id = course.Id,
        Code = course.Code,
        Title = course.Title,
        CreditHours = course.CreditHours
    };
}
