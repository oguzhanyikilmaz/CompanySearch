using CompanySearch.Application.Abstractions.Common;

namespace CompanySearch.Infrastructure.Common;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
