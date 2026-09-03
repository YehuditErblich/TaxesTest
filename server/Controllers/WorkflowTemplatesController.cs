using Microsoft.AspNetCore.Mvc;
using server.Application.Workflows;

namespace server.Controllers;

[ApiController]
[Route("api/workflow-templates")]
public sealed class WorkflowTemplatesController(IWorkflowTemplateService workflowTemplateService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WorkflowTemplateListItem>>> List(
        CancellationToken cancellationToken)
    {
        return Ok(await workflowTemplateService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkflowTemplateDetails>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var template = await workflowTemplateService.GetAsync(id, cancellationToken);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowTemplateDetails>> Create(
        CreateWorkflowTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var template = await workflowTemplateService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = template.Id }, template);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await workflowTemplateService.PublishAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Workflow template cannot be published",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
