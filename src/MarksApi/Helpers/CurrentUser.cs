using System.Security.Claims;

namespace MarksApi.Helpers;

public static class CurrentUser
{
    public const string TeacherRole = "Teacher";
    public const string StudentRole = "Student";
    public const string StudentIdClaim = "student_id";

    public static string? GetRole(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role);

    public static bool IsTeacher(ClaimsPrincipal user) =>
        string.Equals(GetRole(user), TeacherRole, StringComparison.OrdinalIgnoreCase);

    public static bool IsStudent(ClaimsPrincipal user) =>
        string.Equals(GetRole(user), StudentRole, StringComparison.OrdinalIgnoreCase);

    public static int? GetStudentId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(StudentIdClaim);
        return int.TryParse(value, out var id) ? id : null;
    }

    public static void EnsureTeacher(ClaimsPrincipal user)
    {
        if (!IsTeacher(user))
        {
            throw AppException.Forbidden("Only teachers can perform this action.");
        }
    }

    public static void EnsureCanAccessStudent(ClaimsPrincipal user, int studentId)
    {
        if (IsTeacher(user))
        {
            return;
        }

        if (IsStudent(user) && GetStudentId(user) == studentId)
        {
            return;
        }

        throw AppException.Forbidden("Students may only access their own data.");
    }
}
