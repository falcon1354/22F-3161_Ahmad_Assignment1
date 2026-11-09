using System.ComponentModel.DataAnnotations;

namespace MarksApi.Dtos;

public class CreateEnrollmentRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
}

public class UpdateMarkRequest
{
    [Required]
    [Range(0, 100)]
    public int? Mark { get; set; }
}

public class EnrollmentResponse
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int? Mark { get; set; }
}
