using MarksApi.Data;
using MarksApi.Data.Entities;
using MarksApi.Dtos;
using MarksApi.Helpers;
using MarksApi.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarksApi.Services;

public class StudentService(
    AppDbContext db,
    IOptions<AcademicOptions> academicOptions,
    ILogger<StudentService> logger) : IStudentService
{
    public async Task<PagedResult<StudentResponse>> GetStudentsAsync(
        int? page,
        int? pageSize,
        string? search,
        string? sort,
        CancellationToken cancellationToken)
    {
        var (p, size) = PagingHelper.Normalize(page, pageSize);
        var query = db.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(term) ||
                x.RegistrationNo.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term));
        }

        query = ApplySort(query, sort);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((p - 1) * size)
            .Take(size)
            .Select(x => new StudentResponse
            {
                Id = x.Id,
                RegistrationNo = x.RegistrationNo,
                FullName = x.FullName,
                Email = x.Email
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<StudentResponse>
        {
            Items = items,
            Page = p,
            PageSize = size,
            TotalCount = total
        };
    }

    public async Task<StudentResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var student = await db.Students.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StudentResponse
            {
                Id = x.Id,
                RegistrationNo = x.RegistrationNo,
                FullName = x.FullName,
                Email = x.Email
            })
            .FirstOrDefaultAsync(cancellationToken);

        return student ?? throw AppException.NotFound($"Student {id} was not found.");
    }

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        ValidateStudent(request.RegistrationNo, request.FullName, request.Email);

        var student = new Student
        {
            RegistrationNo = request.RegistrationNo.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Students.Add(student);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbConflictHelper.IsUniqueViolation(ex))
        {
            throw AppException.Conflict(
                "Duplicate student",
                "A student with this registration number or email already exists.");
        }

        logger.LogInformation("Created student {StudentId} ({RegistrationNo})", student.Id, student.RegistrationNo);
        return ToResponse(student);
    }

    public async Task<StudentResponse> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken)
    {
        ValidateStudent(request.RegistrationNo, request.FullName, request.Email);

        var student = await db.Students.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw AppException.NotFound($"Student {id} was not found.");

        student.RegistrationNo = request.RegistrationNo.Trim();
        student.FullName = request.FullName.Trim();
        student.Email = request.Email.Trim().ToLowerInvariant();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (DbConflictHelper.IsUniqueViolation(ex))
        {
            throw AppException.Conflict(
                "Duplicate student",
                "A student with this registration number or email already exists.");
        }

        logger.LogInformation("Updated student {StudentId}", student.Id);
        return ToResponse(student);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var student = await db.Students.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw AppException.NotFound($"Student {id} was not found.");

        var hasEnrollments = await db.Enrollments.AnyAsync(x => x.StudentId == id, cancellationToken);
        if (hasEnrollments)
        {
            throw AppException.Conflict(
                "Student has enrollments",
                "Cannot delete a student that still has enrollments.");
        }

        db.Students.Remove(student);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted student {StudentId}", id);
    }

    public async Task<TranscriptResponse> GetTranscriptAsync(int id, CancellationToken cancellationToken)
    {
        var passMark = academicOptions.Value.PassMark;
        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw AppException.NotFound($"Student {id} was not found.");

        var rows = await db.Enrollments.AsNoTracking()
            .Where(x => x.StudentId == id)
            .Select(x => new
            {
                x.Course.Code,
                x.Course.CreditHours,
                x.Mark
            })
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var courses = rows.Select(x => new TranscriptCourseItem
        {
            Code = x.Code,
            CreditHours = x.CreditHours,
            Mark = x.Mark,
            Passed = x.Mark is null ? null : x.Mark >= passMark
        }).ToList();

        var graded = rows.Where(x => x.Mark is not null).ToList();
        double? weightedAverage = null;
        if (graded.Count > 0)
        {
            var weightedSum = graded.Sum(x => x.Mark!.Value * x.CreditHours);
            var creditSum = graded.Sum(x => x.CreditHours);
            weightedAverage = creditSum == 0 ? null : Math.Round(weightedSum / (double)creditSum, 1);
        }

        return new TranscriptResponse
        {
            StudentId = student.Id,
            RegistrationNo = student.RegistrationNo,
            FullName = student.FullName,
            Courses = courses,
            WeightedAverage = weightedAverage
        };
    }

    private static IQueryable<Student> ApplySort(IQueryable<Student> query, string? sort)
    {
        return (sort ?? "fullName").Trim().ToLowerInvariant() switch
        {
            "registrationno" or "registration_no" => query.OrderBy(x => x.RegistrationNo),
            "email" => query.OrderBy(x => x.Email),
            "id" => query.OrderBy(x => x.Id),
            "fullnameamedesc" => query.OrderByDescending(x => x.FullName),
            _ => query.OrderBy(x => x.FullName)
        };
    }

    private static void ValidateStudent(string registrationNo, string fullName, string email)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(registrationNo))
        {
            errors["registrationNo"] = ["Registration number is required."];
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors["fullName"] = ["Full name is required."];
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors["email"] = ["Email is required."];
        }

        if (errors.Count > 0)
        {
            throw AppException.BadRequest("Validation failed.", errors);
        }
    }

    private static StudentResponse ToResponse(Student student) => new()
    {
        Id = student.Id,
        RegistrationNo = student.RegistrationNo,
        FullName = student.FullName,
        Email = student.Email
    };
}
