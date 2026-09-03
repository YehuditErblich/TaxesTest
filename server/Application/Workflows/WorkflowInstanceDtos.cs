namespace server.Application.Workflows;

public sealed record WorkflowActionRequest(
    string PerformedByUserId,
    string ActionType,
    string? Comment);

public sealed record WorkflowInstanceResponse(
    int Id,
    int FormSubmissionId,
    string Status,
    int CurrentStepOrder,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyCollection<WorkflowStepInstanceResponse> Steps,
    IReadOnlyCollection<WorkflowActionResponse> Actions);

public sealed record WorkflowStepInstanceResponse(
    int Id,
    int WorkflowStepId,
    string AssignedToUserId,
    string Status,
    int StepOrder,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record WorkflowActionResponse(
    int Id,
    int WorkflowStepInstanceId,
    string ActionType,
    string PerformedByUserId,
    DateTimeOffset PerformedAt,
    string? Comment);
