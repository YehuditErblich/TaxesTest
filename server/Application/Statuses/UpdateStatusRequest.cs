namespace server.Application.Statuses;

public sealed record UpdateStatusRequest(
    string DisplayText,
    int DisplayOrder,
    bool IsActive);
