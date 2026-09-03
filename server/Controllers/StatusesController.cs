using Microsoft.AspNetCore.Mvc;
using server.Application.Statuses;

namespace server.Controllers;

[ApiController]
[Route("api/statuses")]
public sealed class StatusesController(IStatusService statusService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<StatusValueResponse>>> List(
        [FromQuery] string? statusType,
        CancellationToken cancellationToken)
    {
        return Ok(await statusService.ListAsync(statusType, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<StatusValueResponse>> Update(
        int id,
        UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await statusService.UpdateAsync(id, request, cancellationToken);
            return status is null ? NotFound() : Ok(status);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(exception.Message);
        }
    }
}
