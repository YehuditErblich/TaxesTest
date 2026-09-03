namespace server.Application.Submissions;

public interface ISubmissionService
{
    Task<SubmissionResponse?> GetAsync(int id, CancellationToken cancellationToken);
    Task<SubmissionResponse> CreateAsync(int formTemplateId, CreateSubmissionRequest request, CancellationToken cancellationToken);
}
