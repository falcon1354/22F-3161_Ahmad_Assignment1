using MarksApi.Dtos;
using MarksApi.Helpers;

namespace MarksApi.Services;

public interface ICourseService
{
    Task<PagedResult<CourseResponse>> GetCoursesAsync(int? page, int? pageSize, string? search, string? sort, CancellationToken cancellationToken);
    Task<CourseResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<CourseResponse> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken);
    Task<CourseResponse> UpdateAsync(int id, UpdateCourseRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<CourseStatsResponse> GetStatsAsync(int courseId, CancellationToken cancellationToken);
}
