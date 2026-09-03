namespace server.Application.Statuses;

public sealed record StatusValueResponse(
    int Id,
    string StatusTypeCode,
    string ValueCode,
    string DisplayText,
    int DisplayOrder);
