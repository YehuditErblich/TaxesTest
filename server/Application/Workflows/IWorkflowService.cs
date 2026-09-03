namespace server.Application.Workflows;

public interface IWorkflowService
{
    Task<WorkflowInstanceResponse?> GetAsync(int id, CancellationToken cancellationToken);
    Task<bool> ActAsync(int id, int stepInstanceId, WorkflowActionRequest request, CancellationToken cancellationToken);
}
