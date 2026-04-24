using CompanySearch.Domain.Common;
using CompanySearch.Domain.Enums;

namespace CompanySearch.Domain.Entities;

public sealed class SalesEmail : AuditableEntity
{
    private SalesEmail()
    {
    }

    private SalesEmail(
        Guid businessId,
        string subject,
        string body,
        string? recipientEmail,
        string? generatedByModel)
    {
        BusinessId = businessId;
        Subject = subject;
        Body = body;
        RecipientEmail = recipientEmail;
        GeneratedByModel = generatedByModel;
        SentStatus = EmailSentStatus.ReadyToSend;
    }

    public Guid BusinessId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string? RecipientEmail { get; private set; }

    public string? GeneratedByModel { get; private set; }

    public EmailSentStatus SentStatus { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public int RetryCount { get; private set; }

    public Business? Business { get; private set; }

    public static SalesEmail Create(
        Guid businessId,
        string subject,
        string body,
        string? recipientEmail,
        string? generatedByModel)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Email subject is required.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Email body is required.", nameof(body));
        }

        return new SalesEmail(
            businessId,
            subject.Trim(),
            body.Trim(),
            string.IsNullOrWhiteSpace(recipientEmail) ? null : recipientEmail.Trim(),
            string.IsNullOrWhiteSpace(generatedByModel) ? null : generatedByModel.Trim());
    }

    public void UpdateDraft(string subject, string body, string? recipientEmail, string? generatedByModel, DateTime timestampUtc)
    {
        Subject = string.IsNullOrWhiteSpace(subject) ? Subject : subject.Trim();
        Body = string.IsNullOrWhiteSpace(body) ? Body : body.Trim();
        RecipientEmail = string.IsNullOrWhiteSpace(recipientEmail) ? RecipientEmail : recipientEmail.Trim();
        GeneratedByModel = string.IsNullOrWhiteSpace(generatedByModel) ? GeneratedByModel : generatedByModel.Trim();
        SentStatus = EmailSentStatus.ReadyToSend;
        LastError = null;
        Touch(timestampUtc);
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        SentStatus = EmailSentStatus.Sent;
        SentAtUtc = sentAtUtc;
        LastError = null;
        Touch(sentAtUtc);
    }

    public void MarkFailed(string errorMessage, DateTime timestampUtc)
    {
        SentStatus = EmailSentStatus.Failed;
        RetryCount++;
        LastError = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown email delivery error." : errorMessage.Trim();
        Touch(timestampUtc);
    }
}
