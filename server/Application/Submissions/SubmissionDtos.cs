namespace server.Application.Submissions;

public sealed record CreateSubmissionRequest(
    string SubmittedByUserId,
    IReadOnlyCollection<SubmissionFieldValueRequest> Values,
    IReadOnlyCollection<SubmissionSelectedOptionRequest>? SelectedOptions);

public sealed record SubmissionFieldValueRequest(
    string FieldName,
    string? Value);

public sealed record SubmissionSelectedOptionRequest(
    string FieldName,
    IReadOnlyCollection<int> FieldOptionIds);

public sealed record SubmissionResponse(
    int Id,
    int FormTemplateId,
    int? WorkflowInstanceId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SubmittedAt);
