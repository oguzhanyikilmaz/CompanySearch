using System.Reflection;
using CompanySearch.Application.Common.Behaviors;
using CompanySearch.Application.Common.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CompanySearch.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        services.AddScoped<ILeadScoringService, LeadScoringService>();
        services.AddScoped<IWebsiteScoringService, WebsiteScoringService>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestLoggingBehavior<,>));

        return services;
    }
}
