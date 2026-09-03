using server.Domain.Enums;

namespace server.Domain.Entities;

public sealed class FormTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int StatusId { get; set; }
    public int? WorkflowTemplateId { get; set; }
    public StatusValue Status { get; set; } = null!;
    public WorkflowTemplate? WorkflowTemplate { get; set; }
    public ICollection<FormField> Fields { get; set; } = [];
    public ICollection<FormSubmission> Submissions { get; set; } = [];
}

public sealed class FormField
{
    public int Id { get; set; }
    public int FormTemplateId { get; set; }
    public FieldType FieldType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationSettingsJson { get; set; }
    public string? DisplaySettingsJson { get; set; }
    public FormTemplate FormTemplate { get; set; } = null!;
    public ICollection<FieldOption> Options { get; set; } = [];
    public ICollection<FormSubmissionValue> SubmissionValues { get; set; } = [];
    public ICollection<SubmissionSelectedOption> SelectedOptions { get; set; } = [];
    public ICollection<Upload> Uploads { get; set; } = [];
}

public sealed class FieldOption
{
    public int Id { get; set; }
    public int FormFieldId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public FormField FormField { get; set; } = null!;
}

public sealed class FormSubmission
{
    public int Id { get; set; }
    public int FormTemplateId { get; set; }
    public string SubmittedByUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public int StatusId { get; set; }
    public FormTemplate FormTemplate { get; set; } = null!;
    public StatusValue Status { get; set; } = null!;
    public ICollection<FormSubmissionValue> Values { get; set; } = [];
    public ICollection<SubmissionSelectedOption> SelectedOptions { get; set; } = [];
    public ICollection<Upload> Uploads { get; set; } = [];
    public WorkflowInstance? WorkflowInstance { get; set; }
}

public sealed class FormSubmissionValue
{
    public int Id { get; set; }
    public int FormSubmissionId { get; set; }
    public int FormFieldId { get; set; }
    public string? Value { get; set; }
    public FormSubmission FormSubmission { get; set; } = null!;
    public FormField FormField { get; set; } = null!;
}

public sealed class SubmissionSelectedOption
{
    public int Id { get; set; }
    public int FormSubmissionId { get; set; }
    public int FormFieldId { get; set; }
    public int FieldOptionId { get; set; }
    public FormSubmission FormSubmission { get; set; } = null!;
    public FormField FormField { get; set; } = null!;
    public FieldOption FieldOption { get; set; } = null!;
}

public sealed class Upload
{
    public int Id { get; set; }
    public int FormSubmissionId { get; set; }
    public int? FormFieldId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public FormSubmission FormSubmission { get; set; } = null!;
    public FormField? FormField { get; set; }
}
