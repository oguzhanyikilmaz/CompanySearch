namespace CompanySearch.Api.Contracts.Emails;

public sealed record BulkSendEmailsRequest(IReadOnlyCollection<Guid> BusinessIds);
