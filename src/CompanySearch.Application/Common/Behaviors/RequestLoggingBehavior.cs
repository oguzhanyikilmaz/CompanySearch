using MediatR;
using Microsoft.Extensions.Logging;

namespace CompanySearch.Application.Common.Behaviors;

public sealed class RequestLoggingBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = DateTime.UtcNow;

        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        var elapsed = DateTime.UtcNow - startedAt;
        logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds}ms", requestName, elapsed.TotalMilliseconds);

        return response;
    }
}
