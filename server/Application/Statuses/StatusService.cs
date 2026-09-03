using Microsoft.EntityFrameworkCore;
using server.Data;

namespace server.Application.Statuses;

public sealed class StatusService(AppDbContext dbContext) : IStatusService
{
    public async Task<IReadOnlyCollection<StatusValueResponse>> ListAsync(
        string? statusTypeCode,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StatusValues
            .AsNoTracking()
            .Where(status => status.IsActive);

        if (!string.IsNullOrWhiteSpace(statusTypeCode))
        {
            query = query.Where(status => status.StatusType.Code == statusTypeCode);
        }

        return await query
            .OrderBy(status => status.StatusType.Code)
            .ThenBy(status => status.DisplayOrder)
            .Select(status => new StatusValueResponse(
                status.Id,
                status.StatusType.Code,
                status.ValueCode,
                status.DisplayText,
                status.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<StatusValueResponse?> UpdateAsync(
        int id,
        UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayText) || request.DisplayOrder < 0)
        {
            throw new ArgumentException("Status text and a non-negative display order are required.", nameof(request));
        }

        var status = await dbContext.StatusValues
            .Include(value => value.StatusType)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
        if (status is null)
        {
            return null;
        }

        status.DisplayText = request.DisplayText.Trim();
        status.DisplayOrder = request.DisplayOrder;
        status.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new StatusValueResponse(
            status.Id,
            status.StatusType.Code,
            status.ValueCode,
            status.DisplayText,
            status.DisplayOrder);
    }
}
