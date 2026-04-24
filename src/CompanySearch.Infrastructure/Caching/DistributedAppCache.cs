using System.Text.Json;
using CompanySearch.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;

namespace CompanySearch.Infrastructure.Caching;

public sealed class DistributedAppCache(IDistributedCache distributedCache) : IAppCache
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var payload = await distributedCache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(payload, JsonSerializerOptions.Web);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(value, JsonSerializerOptions.Web);
        await distributedCache.SetStringAsync(
            key,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = duration
            },
            cancellationToken);
    }
}
