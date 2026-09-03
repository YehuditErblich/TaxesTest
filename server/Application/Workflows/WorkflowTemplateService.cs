using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Domain.Entities;

namespace server.Application.Workflows;

public sealed class WorkflowTemplateService(AppDbContext dbContext) : IWorkflowTemplateService
{
    private static readonly HashSet<string> SupportedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Approve",
        "Reject",
        "ReturnForCorrection"
    };
    private const string StatusTypeCode = "WorkflowTemplate";
    private const string DraftStatusCode = "DRAFT";
    private const string PublishedStatusCode = "PUBLISHED";

    public async Task<IReadOnlyCollection<WorkflowTemplateListItem>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.WorkflowTemplates
            .AsNoTracking()
            .OrderByDescending(template => template.CreatedAt)
            .Select(template => new WorkflowTemplateListItem(
                template.Id,
                template.Name,
                template.Status.DisplayText,
                template.CreatedAt,
                template.PublishedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowTemplateDetails?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var template = await dbContext.WorkflowTemplates
            .AsNoTracking()
            .Include(candidate => candidate.Status)
            .Include(candidate => candidate.Steps)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return template is null
            ? null
            : new WorkflowTemplateDetails(
                template.Id,
                template.Name,
                template.Description,
                template.Status.DisplayText,
                template.Steps
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
    }

    public async Task<WorkflowTemplateDetails> CreateAsync(
        CreateWorkflowTemplateRequest request,
        CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        var draftStatusId = await GetStatusIdAsync(StatusTypeCode, DraftStatusCode, cancellationToken);
        var template = new WorkflowTemplate
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

        dbContext.WorkflowTemplates.Add(template);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(template.Id, cancellationToken))!;
    }

    public async Task<bool> PublishAsync(int id, CancellationToken cancellationToken)
    {
        var template = await dbContext.WorkflowTemplates
            .Include(candidate => candidate.Steps)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (template is null)
        {
            return false;
        }

        var publishedStatusId = await GetStatusIdAsync(StatusTypeCode, PublishedStatusCode, cancellationToken);
        if (template.StatusId == publishedStatusId)
        {
            throw new InvalidOperationException("A published workflow template cannot be modified.");
        }

        if (string.IsNullOrWhiteSpace(template.Name) || template.Steps.Count == 0)
        {
            throw new InvalidOperationException("A workflow template requires a name and at least one step before publication.");
        }

        template.PublishedAt = DateTimeOffset.UtcNow;
        template.StatusId = publishedStatusId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<int> GetStatusIdAsync(string typeCode, string valueCode, CancellationToken cancellationToken)
    {
        var statusId = await dbContext.StatusValues
            .Where(status => status.StatusType.Code == typeCode && status.ValueCode == valueCode && status.IsActive)
            .Select(status => (int?)status.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return statusId ?? throw new InvalidOperationException($"Status '{valueCode}' for type '{typeCode}' is not configured.");
    }

    internal static void ValidateRequest(CreateWorkflowTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.CreatedByUserId))
        {
            throw new ArgumentException("A workflow name and creator user ID are required.", nameof(request));
        }

        if (request.Steps.Count == 0)
        {
            throw new ArgumentException("At least one workflow step is required.", nameof(request));
        }

        if (request.Steps.GroupBy(step => step.StepOrder).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Workflow step orders must be unique.", nameof(request));
        }

        if (request.Steps.Any(step => step.StepOrder < 0 || string.IsNullOrWhiteSpace(step.Name)))
        {
            throw new ArgumentException("Each workflow step requires a name and non-negative step order.", nameof(request));
        }

        foreach (var step in request.Steps)
        {
            var hasUser = !string.IsNullOrWhiteSpace(step.ApproverUserId);
            var hasRole = step.ApproverRoleId.HasValue;
            var isUserApprover = string.Equals(step.ApproverType, "User", StringComparison.OrdinalIgnoreCase);
            var isRoleApprover = string.Equals(step.ApproverType, "Role", StringComparison.OrdinalIgnoreCase);
            if (!isUserApprover && !isRoleApprover)
            {
                throw new ArgumentException("Approver type must be User or Role.", nameof(request));
            }

            if (hasUser == hasRole ||
                (isUserApprover && !hasUser) ||
                (isRoleApprover && !hasRole) ||
                step.AllowedActions.Count == 0 ||
                step.AllowedActions.Any(action => !SupportedActions.Contains(action)))
            {
                throw new ArgumentException(
                    "Each step must have one matching approver and at least one supported allowed action.",
                    nameof(request));
            }
        }
    }
}
