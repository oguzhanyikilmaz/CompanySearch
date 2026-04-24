namespace CompanySearch.Application.Abstractions.Caching;

public interface IAppCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);

    Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken);
}
