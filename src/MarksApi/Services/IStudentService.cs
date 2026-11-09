using MarksApi.Dtos;
using MarksApi.Helpers;

namespace MarksApi.Services;

public interface IStudentService
{
    Task<PagedResult<StudentResponse>> GetStudentsAsync(int? page, int? pageSize, string? search, string? sort, CancellationToken cancellationToken);
    Task<StudentResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken);
    Task<StudentResponse> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<TranscriptResponse> GetTranscriptAsync(int id, CancellationToken cancellationToken);
}
