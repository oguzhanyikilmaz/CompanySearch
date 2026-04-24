namespace CompanySearch.Application.Common.Models;

public sealed record EmailSendResult(bool Success, string? MessageId, string? ErrorMessage);
