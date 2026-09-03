namespace server.Domain.Entities;

public sealed class WorkflowTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int StatusId { get; set; }
    public StatusValue Status { get; set; } = null!;
    public ICollection<WorkflowStep> Steps { get; set; } = [];
    public ICollection<FormTemplate> FormTemplates { get; set; } = [];
    public ICollection<WorkflowInstance> Instances { get; set; } = [];
}

public sealed class WorkflowStep
{
    public int Id { get; set; }
    public int WorkflowTemplateId { get; set; }
    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ApproverType { get; set; } = string.Empty;
    public string? ApproverUserId { get; set; }
    public int? ApproverRoleId { get; set; }
    public bool IsRequired { get; set; }
    public string AllowedActionsJson { get; set; } = "[]";
    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;
    public ICollection<WorkflowStepInstance> Instances { get; set; } = [];
}

public sealed class WorkflowInstance
{
    public int Id { get; set; }
    public int FormSubmissionId { get; set; }
    public int WorkflowTemplateId { get; set; }
    public int CurrentStepOrder { get; set; }
    public int StatusId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public FormSubmission FormSubmission { get; set; } = null!;
    public WorkflowTemplate WorkflowTemplate { get; set; } = null!;
    public StatusValue Status { get; set; } = null!;
    public ICollection<WorkflowStepInstance> Steps { get; set; } = [];
    public ICollection<WorkflowAction> Actions { get; set; } = [];
}

public sealed class WorkflowStepInstance
{
    public int Id { get; set; }
    public int WorkflowInstanceId { get; set; }
    public int WorkflowStepId { get; set; }
    public string AssignedToUserId { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    public WorkflowStep WorkflowStep { get; set; } = null!;
    public StatusValue Status { get; set; } = null!;
    public ICollection<WorkflowAction> Actions { get; set; } = [];
}

public sealed class WorkflowAction
{
    public int Id { get; set; }
    public int WorkflowInstanceId { get; set; }
    public int WorkflowStepInstanceId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string PerformedByUserId { get; set; } = string.Empty;
    public DateTimeOffset PerformedAt { get; set; }
    public string? Comment { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    public WorkflowStepInstance WorkflowStepInstance { get; set; } = null!;
}
