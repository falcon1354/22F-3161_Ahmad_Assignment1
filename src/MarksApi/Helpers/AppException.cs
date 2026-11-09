namespace MarksApi.Helpers;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public AppException(int statusCode, string title, string detail, IDictionary<string, string[]>? errors = null)
        : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
        Errors = errors;
    }

    public static AppException BadRequest(string detail, IDictionary<string, string[]>? errors = null) =>
        new(StatusCodes.Status400BadRequest, "One or more validation errors occurred.", detail, errors);

    public static AppException Unauthorized(string detail = "Invalid username or password.") =>
        new(StatusCodes.Status401Unauthorized, "Unauthorized", detail);

    public static AppException Forbidden(string detail = "You are not allowed to access this resource.") =>
        new(StatusCodes.Status403Forbidden, "Forbidden", detail);

    public static AppException NotFound(string detail) =>
        new(StatusCodes.Status404NotFound, "Not Found", detail);

    public static AppException Conflict(string title, string detail) =>
        new(StatusCodes.Status409Conflict, title, detail);
}
