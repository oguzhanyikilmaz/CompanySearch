namespace CompanySearch.Domain.Common;

public abstract class AuditableEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; protected set; }

    public void Touch(DateTime timestampUtc)
    {
        UpdatedAtUtc = timestampUtc;
    }
}
