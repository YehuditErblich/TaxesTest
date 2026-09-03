using Microsoft.AspNetCore.Mvc;
using server.Application.Workflows;

namespace server.Controllers;

[ApiController]
[Route("api/workflows")]
public sealed class WorkflowsController(IWorkflowService workflowService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkflowInstanceResponse>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var workflow = await workflowService.GetAsync(id, cancellationToken);
        return workflow is null ? NotFound() : Ok(workflow);
    }

    [HttpPost("{id:int}/steps/{stepInstanceId:int}/actions")]
    public async Task<IActionResult> Act(
        int id,
        int stepInstanceId,
        WorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await workflowService.ActAsync(id, stepInstanceId, request, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message });
        }
    }
}
