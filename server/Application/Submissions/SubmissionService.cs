using System.Globalization;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Domain.Entities;
using server.Domain.Enums;

namespace server.Application.Submissions;

public sealed class SubmissionService(AppDbContext dbContext) : ISubmissionService
{
    public async Task<SubmissionResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.FormSubmissions
            .AsNoTracking()
            .Where(submission => submission.Id == id)
            .Select(submission => new SubmissionResponse(
                submission.Id,
                submission.FormTemplateId,
                submission.WorkflowInstance == null ? null : submission.WorkflowInstance.Id,
                submission.Status.DisplayText,
                submission.CreatedAt,
                submission.SubmittedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SubmissionResponse> CreateAsync(
        int formTemplateId,
        CreateSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SubmittedByUserId))
        {
            throw new ArgumentException("A submitting user ID is required.", nameof(request));
        }

        var template = await dbContext.FormTemplates
            .AsNoTracking()
            .Include(candidate => candidate.Status)
            .Include(candidate => candidate.Fields)
                .ThenInclude(field => field.Options)
            .Include(candidate => candidate.WorkflowTemplate)
                .ThenInclude(workflow => workflow!.Status)
            .Include(candidate => candidate.WorkflowTemplate)
                .ThenInclude(workflow => workflow!.Steps)
            .SingleOrDefaultAsync(candidate => candidate.Id == formTemplateId, cancellationToken);

        if (template is null)
        {
            throw new KeyNotFoundException("The form template was not found.");
        }

        if (!string.Equals(template.Status.ValueCode, "PUBLISHED", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A form based on an unpublished template cannot be submitted.");
        }

        var fieldsByName = template.Fields.ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
        var valuesByName = request.Values.ToDictionary(value => value.FieldName, StringComparer.OrdinalIgnoreCase);
        var submissionValues = new List<FormSubmissionValue>();
        foreach (var field in template.Fields)
        {
            valuesByName.TryGetValue(field.Name, out var input);
            if (field.IsRequired && string.IsNullOrWhiteSpace(input?.Value))
            {
                throw new ArgumentException($"A value is required for field '{field.Name}'.", nameof(request));
            }

            if (input is not null && !IsValidValue(field.FieldType, input.Value))
            {
                throw new ArgumentException($"The value for field '{field.Name}' is invalid.", nameof(request));
            }

            if (input is not null)
            {
                submissionValues.Add(new FormSubmissionValue
                {
                    FormFieldId = field.Id,
                    Value = input.Value
                });
            }
        }

        var selectedOptions = new List<SubmissionSelectedOption>();
        foreach (var selection in request.SelectedOptions ?? [])
        {
            if (!fieldsByName.TryGetValue(selection.FieldName, out var field) ||
                (field.FieldType is not FieldType.Select and not FieldType.Radio and not FieldType.MultiSelect))
            {
                throw new ArgumentException($"Selection field '{selection.FieldName}' is invalid.", nameof(request));
            }

            if (field.FieldType is not FieldType.MultiSelect && selection.FieldOptionIds.Count > 1)
            {
                throw new ArgumentException($"Field '{field.Name}' accepts only one selected option.", nameof(request));
            }

            var activeOptionIds = field.Options.Where(option => option.IsActive).Select(option => option.Id).ToHashSet();
            if (selection.FieldOptionIds.Any(optionId => !activeOptionIds.Contains(optionId)))
            {
                throw new ArgumentException($"One or more selected options for '{field.Name}' are invalid.", nameof(request));
            }

            selectedOptions.AddRange(selection.FieldOptionIds.Select(optionId => new SubmissionSelectedOption
            {
                FormFieldId = field.Id,
                FieldOptionId = optionId
            }));
        }

        var statusId = await dbContext.StatusValues
            .Where(status => status.StatusType.Code == "FormSubmission" && status.ValueCode == "SUBMITTED" && status.IsActive)
            .Select(status => (int?)status.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Submitted status is not configured.");

        var now = DateTimeOffset.UtcNow;
        var submission = new FormSubmission
        {
            FormTemplateId = formTemplateId,
            SubmittedByUserId = request.SubmittedByUserId.Trim(),
            CreatedAt = now,
            SubmittedAt = now,
            StatusId = statusId,
            Values = submissionValues,
            SelectedOptions = selectedOptions
        };

        if (template.WorkflowTemplate is not null &&
            !string.Equals(template.WorkflowTemplate.Status.ValueCode, "PUBLISHED", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A form cannot use an unpublished workflow template.");
        }

        if (template.WorkflowTemplate?.Steps.Any(step => string.IsNullOrWhiteSpace(step.ApproverUserId)) == true)
        {
            throw new InvalidOperationException("Role-assigned workflow steps require an identity assignment before submission.");
        }

        dbContext.FormSubmissions.Add(submission);

        if (template.WorkflowTemplate is not null)
        {
            var workflowStatusId = await GetStatusIdAsync("WorkflowInstance", "IN_PROGRESS", cancellationToken);
            var stepStatusId = await GetStatusIdAsync("WorkflowStepInstance", "IN_PROGRESS", cancellationToken);
            var pendingStepStatusId = await GetStatusIdAsync("WorkflowStepInstance", "DRAFT", cancellationToken);
            var firstStep = template.WorkflowTemplate.Steps.OrderBy(step => step.StepOrder).First();
            submission.WorkflowInstance = new WorkflowInstance
            {
                WorkflowTemplateId = template.WorkflowTemplate.Id,
                CurrentStepOrder = firstStep.StepOrder,
                StatusId = workflowStatusId,
                StartedAt = now,
                Steps = template.WorkflowTemplate.Steps.OrderBy(step => step.StepOrder).Select(step => new WorkflowStepInstance
                {
                    WorkflowStepId = step.Id,
                    AssignedToUserId = step.ApproverUserId!,
                    StatusId = step.StepOrder == firstStep.StepOrder ? stepStatusId : pendingStepStatusId,
                    StartedAt = now
                }).ToList()
            };
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(submission.Id, cancellationToken))!;
    }

    private async Task<int> GetStatusIdAsync(string statusTypeCode, string valueCode, CancellationToken cancellationToken)
    {
        return await dbContext.StatusValues
            .Where(status => status.StatusType.Code == statusTypeCode && status.ValueCode == valueCode && status.IsActive)
            .Select(status => (int?)status.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Status '{valueCode}' for type '{statusTypeCode}' is not configured.");
    }

    private static bool IsValidValue(FieldType fieldType, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return fieldType switch
        {
            FieldType.Number => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            FieldType.Date => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            FieldType.Checkbox => bool.TryParse(value, out _),
            _ => true
        };
    }
}
