using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CompanySearch.Infrastructure.Persistence;

internal static class JsonValueConverters
{
    public static ValueConverter<T, string> CreateConverter<T>()
    {
        return new ValueConverter<T, string>(
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Web),
            value => JsonSerializer.Deserialize<T>(value, JsonSerializerOptions.Web)!);
    }

    public static ValueComparer<T> CreateComparer<T>()
    {
        return new ValueComparer<T>(
            (left, right) => JsonSerializer.Serialize(left, JsonSerializerOptions.Web) == JsonSerializer.Serialize(right, JsonSerializerOptions.Web),
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Web).GetHashCode(),
            value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!);
    }
}
