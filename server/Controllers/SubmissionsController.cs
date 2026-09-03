using Microsoft.AspNetCore.Mvc;
using server.Application.Submissions;

namespace server.Controllers;

[ApiController]
[Route("api/form-templates/{formTemplateId:int}/submissions")]
public sealed class SubmissionsController(ISubmissionService submissionService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubmissionResponse>> Get(
        int formTemplateId,
        int id,
        CancellationToken cancellationToken)
    {
        var submission = await submissionService.GetAsync(id, cancellationToken);
        return submission is null || submission.FormTemplateId != formTemplateId
            ? NotFound()
            : Ok(submission);
    }

    [HttpPost]
    public async Task<ActionResult<SubmissionResponse>> Create(
        int formTemplateId,
        CreateSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var submission = await submissionService.CreateAsync(formTemplateId, request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { formTemplateId, id = submission.Id }, submission);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails { Detail = exception.Message });
        }
    }
}
