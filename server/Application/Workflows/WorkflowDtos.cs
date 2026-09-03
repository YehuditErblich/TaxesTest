namespace server.Application.Workflows;

public sealed record CreateWorkflowTemplateRequest(
    string Name,
    string? Description,
    string CreatedByUserId,
    IReadOnlyCollection<CreateWorkflowStepRequest> Steps);

public sealed record CreateWorkflowStepRequest(
    int StepOrder,
    string Name,
    string? Description,
    string ApproverType,
    string? ApproverUserId,
    int? ApproverRoleId,
    bool IsRequired,
    IReadOnlyCollection<string> AllowedActions);

public sealed record WorkflowTemplateListItem(
    int Id,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);

public sealed record WorkflowTemplateDetails(
    int Id,
    string Name,
    string? Description,
    string Status,
    IReadOnlyCollection<WorkflowStepDetails> Steps);

public sealed record WorkflowStepDetails(
    int Id,
    int StepOrder,
    string Name,
    string? Description,
    string ApproverType,
    string? ApproverUserId,
    int? ApproverRoleId,
    bool IsRequired,
    IReadOnlyCollection<string> AllowedActions);
