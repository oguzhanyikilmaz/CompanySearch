namespace CompanySearch.Infrastructure.Options;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "EmailDelivery";

    public bool SandboxMode { get; set; } = true;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromName { get; set; } = "CompanySearch";

    public string FromEmail { get; set; } = "noreply@example.com";

    public bool UseSsl { get; set; } = true;

    public int RetryCount { get; set; } = 3;
}
