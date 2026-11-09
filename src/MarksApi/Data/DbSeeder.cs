using MarksApi.Data.Entities;
using MarksApi.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarksApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        DemoUsersOptions demoUsers,
        CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(demoUsers.TeacherPassword) ||
            string.IsNullOrWhiteSpace(demoUsers.StudentPassword))
        {
            throw new InvalidOperationException(
                "DemoUsers passwords are missing. Set DemoUsers:TeacherPassword and DemoUsers:StudentPassword with user-secrets.");
        }

        var now = DateTime.UtcNow;

        var student1 = new Student
        {
            RegistrationNo = "CS-2022-001",
            FullName = "Ali Raza",
            Email = "ali.raza@example.com",
            CreatedAtUtc = now
        };
        var student2 = new Student
        {
            RegistrationNo = "CS-2022-002",
            FullName = "Sara Khan",
            Email = "sara.khan@example.com",
            CreatedAtUtc = now
        };

        var course1 = new Course
        {
            Code = "CS401",
            Title = "Advanced Programming",
            CreditHours = 3,
            CreatedAtUtc = now
        };
        var course2 = new Course
        {
            Code = "MA201",
            Title = "Linear Algebra",
            CreditHours = 2,
            CreatedAtUtc = now
        };
        var course3 = new Course
        {
            Code = "CS402",
            Title = "Databases",
            CreditHours = 3,
            CreatedAtUtc = now
        };

        db.Students.AddRange(student1, student2);
        db.Courses.AddRange(course1, course2, course3);
        await db.SaveChangesAsync(cancellationToken);

        db.Enrollments.AddRange(
            new Enrollment { StudentId = student1.Id, CourseId = course1.Id, Mark = 80, EnrolledAtUtc = now },
            new Enrollment { StudentId = student1.Id, CourseId = course2.Id, Mark = 60, EnrolledAtUtc = now },
            new Enrollment { StudentId = student1.Id, CourseId = course3.Id, Mark = null, EnrolledAtUtc = now },
            new Enrollment { StudentId = student2.Id, CourseId = course1.Id, Mark = 70, EnrolledAtUtc = now },
            new Enrollment { StudentId = student2.Id, CourseId = course2.Id, Mark = null, EnrolledAtUtc = now },
            new Enrollment { StudentId = student2.Id, CourseId = course3.Id, Mark = 90, EnrolledAtUtc = now });

        var teacher = new User
        {
            Username = demoUsers.TeacherUsername,
            Role = "Teacher",
            StudentId = null
        };
        teacher.PasswordHash = passwordHasher.HashPassword(teacher, demoUsers.TeacherPassword);

        var studentUser1 = new User
        {
            Username = demoUsers.StudentUsername,
            Role = "Student",
            StudentId = student1.Id
        };
        studentUser1.PasswordHash = passwordHasher.HashPassword(studentUser1, demoUsers.StudentPassword);

        var studentUser2 = new User
        {
            Username = "student2",
            Role = "Student",
            StudentId = student2.Id
        };
        studentUser2.PasswordHash = passwordHasher.HashPassword(studentUser2, demoUsers.StudentPassword);

        db.Users.AddRange(teacher, studentUser1, studentUser2);
        await db.SaveChangesAsync(cancellationToken);
    }
}
