namespace server.Domain.Entities;

public sealed class StatusType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public ICollection<StatusValue> Values { get; set; } = [];
}

public sealed class StatusValue
{
    public int Id { get; set; }
    public int StatusTypeId { get; set; }
    public string ValueCode { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public StatusType StatusType { get; set; } = null!;
    public ICollection<FormTemplate> FormTemplates { get; set; } = [];
    public ICollection<WorkflowTemplate> WorkflowTemplates { get; set; } = [];
    public ICollection<FormSubmission> FormSubmissions { get; set; } = [];
    public ICollection<WorkflowInstance> WorkflowInstances { get; set; } = [];
    public ICollection<WorkflowStepInstance> WorkflowStepInstances { get; set; } = [];
}
