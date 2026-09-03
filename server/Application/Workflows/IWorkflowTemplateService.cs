namespace server.Application.Workflows;

public interface IWorkflowTemplateService
{
    Task<IReadOnlyCollection<WorkflowTemplateListItem>> ListAsync(CancellationToken cancellationToken);
    Task<WorkflowTemplateDetails?> GetAsync(int id, CancellationToken cancellationToken);
    Task<WorkflowTemplateDetails> CreateAsync(CreateWorkflowTemplateRequest request, CancellationToken cancellationToken);
    Task<bool> PublishAsync(int id, CancellationToken cancellationToken);
}
