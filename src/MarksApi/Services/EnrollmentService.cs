using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos;
using MarksApi.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Services;

public class EnrollmentService(AppDbContext db, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public async Task<EnrollmentResponse> EnrollAsync(
        int courseId,
        CreateEnrollmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StudentId < 1)
        {
            throw AppException.BadRequest(
                "Validation failed.",
                new Dictionary<string, string[]> { ["studentId"] = ["Student id is required."] });
        }

        var courseExists = await db.Courses.AnyAsync(x => x.Id == courseId, cancellationToken);
        if (!courseExists)
        {
            throw AppException.NotFound($"Course {courseId} was not found.");
        }

        var studentExists = await db.Students.AnyAsync(x => x.Id == request.StudentId, cancellationToken);
        if (!studentExists)
        {
            throw AppException.NotFound($"Student {request.StudentId} was not found.");
        }

        var enrollment = new Enrollment
        {
            StudentId = request.StudentId,
            CourseId = courseId,
            Mark = null,
            EnrolledAtUtc = DateTime.UtcNow
        };

        db.Enrollments.Add(enrollment);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbConflictHelper.IsUniqueViolation(ex))
        {
            throw AppException.Conflict(
                "Duplicate enrollment",
                "This student is already enrolled in this course.");
        }

        logger.LogInformation(
            "Enrolled student {StudentId} in course {CourseId} as enrollment {EnrollmentId}",
            enrollment.StudentId,
            enrollment.CourseId,
            enrollment.Id);

        return ToResponse(enrollment);
    }

    public async Task<PagedResult<EnrollmentResponse>> GetByCourseAsync(
        int courseId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var courseExists = await db.Courses.AsNoTracking().AnyAsync(x => x.Id == courseId, cancellationToken);
        if (!courseExists)
        {
            throw AppException.NotFound($"Course {courseId} was not found.");
        }

        var (p, size) = PagingHelper.Normalize(page, pageSize);
        var query = db.Enrollments.AsNoTracking()
            .Where(x => x.CourseId == courseId)
            .OrderBy(x => x.Id);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((p - 1) * size)
            .Take(size)
            .Select(x => new EnrollmentResponse
            {
                Id = x.Id,
                StudentId = x.StudentId,
                CourseId = x.CourseId,
                Mark = x.Mark
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<EnrollmentResponse>
        {
            Items = items,
            Page = p,
            PageSize = size,
            TotalCount = total
        };
    }

    public async Task<EnrollmentResponse> UpdateMarkAsync(
        int enrollmentId,
        UpdateMarkRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Mark is null || request.Mark is < 0 or > 100)
        {
            throw AppException.BadRequest(
                "Validation failed.",
                new Dictionary<string, string[]> { ["mark"] = ["Mark must be a whole number from 0 to 100."] });
        }

        var enrollment = await db.Enrollments.FirstOrDefaultAsync(x => x.Id == enrollmentId, cancellationToken)
            ?? throw AppException.NotFound($"Enrollment {enrollmentId} was not found.");

        enrollment.Mark = request.Mark;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated mark for enrollment {EnrollmentId} to {Mark}",
            enrollment.Id,
            enrollment.Mark);

        return ToResponse(enrollment);
    }

    public async Task DeleteAsync(int enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await db.Enrollments.FirstOrDefaultAsync(x => x.Id == enrollmentId, cancellationToken)
            ?? throw AppException.NotFound($"Enrollment {enrollmentId} was not found.");

        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted enrollment {EnrollmentId}", enrollmentId);
    }

    private static EnrollmentResponse ToResponse(Enrollment enrollment) => new()
    {
        Id = enrollment.Id,
        StudentId = enrollment.StudentId,
        CourseId = enrollment.CourseId,
        Mark = enrollment.Mark
    };
}
