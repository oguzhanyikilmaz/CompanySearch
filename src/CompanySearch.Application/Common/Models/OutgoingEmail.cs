namespace CompanySearch.Application.Common.Models;

public sealed record OutgoingEmail(string RecipientEmail, string Subject, string Body, string BusinessName);
