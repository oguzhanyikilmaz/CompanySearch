namespace CompanySearch.Application.Abstractions.Persistence;

/// <summary>
/// İşletmeler, analizler, e-postalar ve arama işleri dahil uygulama verisini temizler.
/// </summary>
public interface IApplicationDataPurge
{
    Task PurgeAllAsync(CancellationToken cancellationToken);
}
