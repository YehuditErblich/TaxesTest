using server.Domain.Enums;
using server.Application.Workflows;

namespace server.Application.FormTemplates;

public sealed record CreateFormTemplateRequest(
    string Name,
    string? Description,
    string CreatedByUserId,
    int? WorkflowTemplateId,
    CreateWorkflowTemplateRequest? Workflow,
    IReadOnlyCollection<CreateFormFieldRequest> Fields);

public sealed record CreateFormFieldRequest(
    FieldType FieldType,
    string Name,
    string Label,
    string? Placeholder,
    string? HelpText,
    string? DefaultValue,
    bool IsRequired,
    bool IsReadOnly,
    int DisplayOrder,
    string? ValidationSettingsJson,
    string? DisplaySettingsJson,
    IReadOnlyCollection<CreateFieldOptionRequest>? Options);

public sealed record CreateFieldOptionRequest(
    string Value,
    string Label,
    int DisplayOrder);

public sealed record FormTemplateListItem(
    int Id,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);

public sealed record FormTemplateDetails(
    int Id,
    string Name,
    string? Description,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    string Status,
    int? WorkflowTemplateId,
    WorkflowTemplateDetails? Workflow,
    IReadOnlyCollection<FormFieldDetails> Fields);

public sealed record FormFieldDetails(
    int Id,
    FieldType FieldType,
    string Name,
    string Label,
    bool IsRequired,
    int DisplayOrder,
    IReadOnlyCollection<FieldOptionDetails> Options);

public sealed record FieldOptionDetails(
    int Id,
    string Value,
    string Label,
    int DisplayOrder);
