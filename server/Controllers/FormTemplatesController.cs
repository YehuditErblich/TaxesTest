using Microsoft.AspNetCore.Mvc;
using server.Application.FormTemplates;

namespace server.Controllers;

[ApiController]
[Route("api/form-templates")]
public sealed class FormTemplatesController(IFormTemplateService formTemplateService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<FormTemplateListItem>>> List(
        CancellationToken cancellationToken)
    {
        return Ok(await formTemplateService.ListAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FormTemplateDetails>> Get(
        int id,
        CancellationToken cancellationToken)
    {
        var template = await formTemplateService.GetAsync(id, cancellationToken);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<FormTemplateDetails>> Create(
        CreateFormTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var template = await formTemplateService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = template.Id }, template);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            return await formTemplateService.PublishAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Form template cannot be published",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
