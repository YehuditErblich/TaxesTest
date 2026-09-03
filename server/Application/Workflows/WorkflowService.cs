using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Domain.Entities;

namespace server.Application.Workflows;

public sealed class WorkflowService(AppDbContext dbContext) : IWorkflowService
{
    public async Task<WorkflowInstanceResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        var instance = await dbContext.WorkflowInstances
            .AsNoTracking()
            .Include(workflow => workflow.Status)
            .Include(workflow => workflow.Steps)
                .ThenInclude(step => step.Status)
            .Include(workflow => workflow.Steps)
                .ThenInclude(step => step.WorkflowStep)
            .Include(workflow => workflow.Actions)
            .SingleOrDefaultAsync(workflow => workflow.Id == id, cancellationToken);

        return instance is null
            ? null
            : new WorkflowInstanceResponse(
                instance.Id,
                instance.FormSubmissionId,
                instance.Status.DisplayText,
                instance.CurrentStepOrder,
                instance.StartedAt,
                instance.CompletedAt,
                instance.Steps
                    .OrderBy(step => step.WorkflowStep.StepOrder)
                    .Select(step => new WorkflowStepInstanceResponse(
                        step.Id,
                        step.WorkflowStepId,
                        step.AssignedToUserId,
                        step.Status.DisplayText,
                        step.WorkflowStep.StepOrder,
                        step.StartedAt,
                        step.CompletedAt))
                    .ToList(),
                instance.Actions
                    .OrderBy(action => action.PerformedAt)
                    .Select(action => new WorkflowActionResponse(
                        action.Id,
                        action.WorkflowStepInstanceId,
                        action.ActionType,
                        action.PerformedByUserId,
                        action.PerformedAt,
                        action.Comment))
                    .ToList());
    }

    public async Task<bool> ActAsync(
        int id,
        int stepInstanceId,
        WorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PerformedByUserId) || string.IsNullOrWhiteSpace(request.ActionType))
        {
            throw new ArgumentException("An acting user and action type are required.", nameof(request));
        }

        var workflow = await dbContext.WorkflowInstances
            .Include(instance => instance.Status)
            .Include(instance => instance.FormSubmission)
                .ThenInclude(submission => submission.Status)
            .Include(instance => instance.Steps)
                .ThenInclude(step => step.Status)
            .Include(instance => instance.Steps)
                .ThenInclude(step => step.WorkflowStep)
            .SingleOrDefaultAsync(instance => instance.Id == id, cancellationToken);

        if (workflow is null)
        {
            return false;
        }

        var step = workflow.Steps.SingleOrDefault(candidate => candidate.Id == stepInstanceId);
        if (step is null)
        {
            throw new KeyNotFoundException("The workflow step was not found.");
        }

        if (!string.Equals(step.AssignedToUserId, request.PerformedByUserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Only the assigned approver may perform this action.");
        }

        var allowedActions = JsonSerializer.Deserialize<HashSet<string>>(step.WorkflowStep.AllowedActionsJson)
            ?? [];
        if (!allowedActions.Contains(request.ActionType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The action is not allowed for the current workflow step.", nameof(request));
        }

        if (step.WorkflowStep.StepOrder != workflow.CurrentStepOrder || step.CompletedAt is not null)
        {
            throw new InvalidOperationException("The workflow step is not active.");
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.WorkflowActions.Add(new WorkflowAction
        {
            WorkflowInstanceId = workflow.Id,
            WorkflowStepInstanceId = step.Id,
            ActionType = request.ActionType.Trim(),
            PerformedByUserId = request.PerformedByUserId.Trim(),
            PerformedAt = now,
            Comment = request.Comment?.Trim()
        });

        step.CompletedAt = now;
        var completedStepStatusCode = request.ActionType.Equals("Approve", StringComparison.OrdinalIgnoreCase)
            ? "APPROVED"
            : request.ActionType.Equals("ReturnForCorrection", StringComparison.OrdinalIgnoreCase)
                ? "RETURNED_FOR_CORRECTION"
                : "REJECTED";
        step.StatusId = await GetStatusIdAsync("WorkflowStepInstance", completedStepStatusCode, cancellationToken);
        var nextStep = workflow.Steps
            .Where(candidate => candidate.WorkflowStep.StepOrder > workflow.CurrentStepOrder)
            .OrderBy(candidate => candidate.WorkflowStep.StepOrder)
            .FirstOrDefault();

        if (string.Equals(request.ActionType, "Approve", StringComparison.OrdinalIgnoreCase) && nextStep is not null)
        {
            workflow.CurrentStepOrder = nextStep.WorkflowStep.StepOrder;
            nextStep.StatusId = await GetStatusIdAsync("WorkflowStepInstance", "IN_PROGRESS", cancellationToken);
        }
        else
        {
            workflow.CompletedAt = now;
            var finalStatusCode = request.ActionType.Equals("Approve", StringComparison.OrdinalIgnoreCase)
                ? "APPROVED"
                : request.ActionType.Equals("ReturnForCorrection", StringComparison.OrdinalIgnoreCase)
                    ? "RETURNED_FOR_CORRECTION"
                    : "REJECTED";
            workflow.StatusId = await GetStatusIdAsync("WorkflowInstance", finalStatusCode, cancellationToken);
            workflow.FormSubmission.StatusId = await GetStatusIdAsync("FormSubmission", finalStatusCode, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<int> GetStatusIdAsync(string statusTypeCode, string valueCode, CancellationToken cancellationToken)
    {
        return await dbContext.StatusValues
            .Where(status => status.StatusType.Code == statusTypeCode && status.ValueCode == valueCode && status.IsActive)
            .Select(status => (int?)status.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Status '{valueCode}' for type '{statusTypeCode}' is not configured.");
    }
}
