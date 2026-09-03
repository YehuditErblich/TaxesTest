namespace server.Application.FormTemplates;

public interface IFormTemplateService
{
    Task<IReadOnlyCollection<FormTemplateListItem>> ListAsync(CancellationToken cancellationToken);
    Task<FormTemplateDetails?> GetAsync(int id, CancellationToken cancellationToken);
    Task<FormTemplateDetails> CreateAsync(CreateFormTemplateRequest request, CancellationToken cancellationToken);
    Task<bool> PublishAsync(int id, CancellationToken cancellationToken);
}
