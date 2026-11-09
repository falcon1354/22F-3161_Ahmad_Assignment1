namespace MarksApi.Options;

public class DemoUsersOptions
{
    public const string SectionName = "DemoUsers";

    public string TeacherUsername { get; set; } = "teacher1";
    public string TeacherPassword { get; set; } = string.Empty;
    public string StudentUsername { get; set; } = "student1";
    public string StudentPassword { get; set; } = string.Empty;
}
