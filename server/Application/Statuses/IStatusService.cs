namespace server.Application.Statuses;

public interface IStatusService
{
    Task<IReadOnlyCollection<StatusValueResponse>> ListAsync(string? statusTypeCode, CancellationToken cancellationToken);
    Task<StatusValueResponse?> UpdateAsync(int id, UpdateStatusRequest request, CancellationToken cancellationToken);
}
