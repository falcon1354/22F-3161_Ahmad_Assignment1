using System.ComponentModel.DataAnnotations;

namespace MarksApi.Dtos;

public class CreateStudentRequest
{
    [Required]
    [MaxLength(20)]
    public string RegistrationNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}

public class UpdateStudentRequest
{
    [Required]
    [MaxLength(20)]
    public string RegistrationNo { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}

public class StudentResponse
{
    public int Id { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class TranscriptCourseItem
{
    public string Code { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public int? Mark { get; set; }
    public bool? Passed { get; set; }
}

public class TranscriptResponse
{
    public int StudentId { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<TranscriptCourseItem> Courses { get; set; } = new();
    public double? WeightedAverage { get; set; }
}
