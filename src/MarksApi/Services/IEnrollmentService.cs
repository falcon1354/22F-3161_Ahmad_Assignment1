using MarksApi.Dtos;
using MarksApi.Helpers;

namespace MarksApi.Services;

public interface IEnrollmentService
{
    Task<EnrollmentResponse> EnrollAsync(int courseId, CreateEnrollmentRequest request, CancellationToken cancellationToken);
    Task<PagedResult<EnrollmentResponse>> GetByCourseAsync(int courseId, int? page, int? pageSize, CancellationToken cancellationToken);
    Task<EnrollmentResponse> UpdateMarkAsync(int enrollmentId, UpdateMarkRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int enrollmentId, CancellationToken cancellationToken);
}
