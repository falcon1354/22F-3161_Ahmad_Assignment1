using System.ComponentModel.DataAnnotations;

namespace MarksApi.Dtos;

public class CreateCourseRequest
{
    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 4)]
    public int CreditHours { get; set; }
}

public class UpdateCourseRequest
{
    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Range(1, 4)]
    public int CreditHours { get; set; }
}

public class CourseResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CreditHours { get; set; }
}

public class CourseStatsResponse
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public int Enrolled { get; set; }
    public int Graded { get; set; }
    public double? Average { get; set; }
    public int? Highest { get; set; }
    public int? Lowest { get; set; }
    public int Passed { get; set; }
}
