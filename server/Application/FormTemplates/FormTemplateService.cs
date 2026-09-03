using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Application.Workflows;
using server.Data;
using server.Domain.Entities;

namespace server.Application.FormTemplates;

public sealed class FormTemplateService(AppDbContext dbContext) : IFormTemplateService
{
    private const string FormTemplateStatusType = "FormTemplate";
    private const string DraftStatus = "DRAFT";
    private const string PublishedStatus = "PUBLISHED";

    public async Task<IReadOnlyCollection<FormTemplateListItem>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.FormTemplates
            .AsNoTracking()
            .OrderByDescending(template => template.CreatedAt)
            .Select(template => new FormTemplateListItem(
                template.Id,
                template.Name,
                template.Status.DisplayText,
                template.CreatedAt,
                template.PublishedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<FormTemplateDetails?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var template = await dbContext.FormTemplates
            .AsNoTracking()
            .Include(candidate => candidate.Status)
            .Include(candidate => candidate.Fields)
                .ThenInclude(field => field.Options)
            .Include(candidate => candidate.WorkflowTemplate)
                .ThenInclude(workflow => workflow!.Status)
            .Include(candidate => candidate.WorkflowTemplate)
                .ThenInclude(workflow => workflow!.Steps)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (template is null)
        {
            return null;
        }

        var workflow = template.WorkflowTemplate is null
            ? null
            : new WorkflowTemplateDetails(
                template.WorkflowTemplate.Id,
                template.WorkflowTemplate.Name,
                template.WorkflowTemplate.Description,
                template.WorkflowTemplate.Status.DisplayText,
                template.WorkflowTemplate.Steps
                    .OrderBy(step => step.StepOrder)
                    .Select(step => new WorkflowStepDetails(
                        step.Id,
                        step.StepOrder,
                        step.Name,
                        step.Description,
                        step.ApproverType,
                        step.ApproverUserId,
                        step.ApproverRoleId,
                        step.IsRequired,
                        JsonSerializer.Deserialize<IReadOnlyCollection<string>>(step.AllowedActionsJson) ?? []))
                    .ToList());

        return new FormTemplateDetails(
            template.Id,
            template.Name,
            template.Description,
            template.CreatedByUserId,
            template.CreatedAt,
            template.PublishedAt,
            template.Status.DisplayText,
            template.WorkflowTemplateId,
            workflow,
            template.Fields
                .OrderBy(field => field.DisplayOrder)
                .Select(field => new FormFieldDetails(
                    field.Id,
                    field.FieldType,
                    field.Name,
                    field.Label,
                    field.IsRequired,
                    field.DisplayOrder,
                    field.Options
                        .Where(option => option.IsActive)
                        .OrderBy(option => option.DisplayOrder)
                        .Select(option => new FieldOptionDetails(
                            option.Id,
                            option.Value,
                            option.Label,
                            option.DisplayOrder))
                        .ToList()))
                .ToList());
    }

    public async Task<FormTemplateDetails> CreateAsync(
        CreateFormTemplateRequest request,
        CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);
        var draftStatusId = await GetStatusIdAsync(FormTemplateStatusType, DraftStatus, cancellationToken);
        var workflowDraftStatusId = request.Workflow is null
            ? (int?)null
            : await GetStatusIdAsync("WorkflowTemplate", DraftStatus, cancellationToken);

        WorkflowTemplate? workflow = null;
        if (request.Workflow is not null)
        {
            WorkflowTemplateService.ValidateRequest(request.Workflow);
            workflow = CreateWorkflowTemplate(request.Workflow, workflowDraftStatusId!.Value);
        }

        var template = new FormTemplate
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedByUserId = request.CreatedByUserId.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            StatusId = draftStatusId,
            WorkflowTemplateId = request.WorkflowTemplateId,
            WorkflowTemplate = workflow,
            Fields = request.Fields.Select(field => new FormField
            {
                FieldType = field.FieldType,
                Name = field.Name.Trim(),
                Label = field.Label.Trim(),
                Placeholder = field.Placeholder?.Trim(),
                HelpText = field.HelpText?.Trim(),
                DefaultValue = field.DefaultValue,
                IsRequired = field.IsRequired,
                IsReadOnly = field.IsReadOnly,
                DisplayOrder = field.DisplayOrder,
                ValidationSettingsJson = field.ValidationSettingsJson,
                DisplaySettingsJson = field.DisplaySettingsJson,
                Options = (field.Options ?? []).Select(option => new FieldOption
                {
                    Value = option.Value.Trim(),
                    Label = option.Label.Trim(),
                    DisplayOrder = option.DisplayOrder,
                    IsActive = true
                }).ToList()
            }).ToList()
        };

        dbContext.FormTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(template.Id, cancellationToken))!;
    }

    private static WorkflowTemplate CreateWorkflowTemplate(
        CreateWorkflowTemplateRequest request,
        int draftStatusId)
    {
        return new WorkflowTemplate
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedByUserId = request.CreatedByUserId.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            StatusId = draftStatusId,
            Steps = request.Steps.Select(step => new WorkflowStep
            {
                StepOrder = step.StepOrder,
                Name = step.Name.Trim(),
                Description = step.Description?.Trim(),
                ApproverType = step.ApproverType.Trim(),
                ApproverUserId = step.ApproverUserId?.Trim(),
                ApproverRoleId = step.ApproverRoleId,
                IsRequired = step.IsRequired,
                AllowedActionsJson = JsonSerializer.Serialize(step.AllowedActions)
            }).ToList()
        };
    }

    public async Task<bool> PublishAsync(int id, CancellationToken cancellationToken)
    {
        var template = await dbContext.FormTemplates
            .Include(candidate => candidate.Fields)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (template is null)
        {
            return false;
        }

        var publishedStatusId = await GetStatusIdAsync(FormTemplateStatusType, PublishedStatus, cancellationToken);
        if (template.StatusId == publishedStatusId)
        {
            throw new InvalidOperationException("A published form template cannot be modified.");
        }

        if (string.IsNullOrWhiteSpace(template.Name) || template.Fields.Count == 0)
        {
            throw new InvalidOperationException("A form template requires a name and at least one field before publication.");
        }

        template.PublishedAt = DateTimeOffset.UtcNow;
        template.StatusId = publishedStatusId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<int> GetStatusIdAsync(
        string statusTypeCode,
        string valueCode,
        CancellationToken cancellationToken)
    {
        var statusId = await dbContext.StatusValues
            .Where(status => status.StatusType.Code == statusTypeCode &&
                             status.ValueCode == valueCode &&
                             status.IsActive)
            .Select(status => (int?)status.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return statusId ?? throw new InvalidOperationException(
            $"Status '{valueCode}' for type '{statusTypeCode}' is not configured.");
    }

    private static void ValidateCreateRequest(CreateFormTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A form template name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.CreatedByUserId))
        {
            throw new ArgumentException("A creator user ID is required.", nameof(request));
        }

        if (request.Fields.Count == 0)
        {
            throw new ArgumentException("At least one form field is required.", nameof(request));
        }

        if (request.WorkflowTemplateId.HasValue && request.Workflow is not null)
        {
            throw new ArgumentException(
                "Specify either an existing workflow template or a new workflow, not both.",
                nameof(request));
        }

        var duplicateNames = request.Fields
            .GroupBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);
        var duplicateOrders = request.Fields
            .GroupBy(field => field.DisplayOrder)
            .Any(group => group.Count() > 1);
        if (duplicateNames || duplicateOrders)
        {
            throw new ArgumentException("Form field names and display orders must be unique.", nameof(request));
        }

        if (request.Fields.Any(field => field.DisplayOrder < 0 ||
                                        !Enum.IsDefined(field.FieldType) ||
                                        string.IsNullOrWhiteSpace(field.Name) ||
                                        string.IsNullOrWhiteSpace(field.Label)))
        {
            throw new ArgumentException("Each form field requires a name, label, and non-negative display order.", nameof(request));
        }

        foreach (var field in request.Fields.Where(field => field.Options is not null))
        {
            var duplicateOptions = field.Options!
                .GroupBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
                .Any(group => group.Count() > 1);
            if (duplicateOptions)
            {
                throw new ArgumentException("Field option values must be unique within a field.", nameof(request));
            }
        }
    }
}
