using CompanySearch.Application.Abstractions.AI;
using CompanySearch.Application.Abstractions.Caching;
using CompanySearch.Application.Abstractions.Common;
using CompanySearch.Application.Abstractions.Email;
using CompanySearch.Application.Abstractions.Geocoding;
using CompanySearch.Application.Abstractions.Jobs;
using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Abstractions.Websites;
using CompanySearch.Infrastructure.Caching;
using CompanySearch.Infrastructure.Common;
using CompanySearch.Infrastructure.Email;
using CompanySearch.Infrastructure.External.OpenAI;
using CompanySearch.Infrastructure.External.OpenStreetMap;
using CompanySearch.Infrastructure.Jobs;
using CompanySearch.Infrastructure.Options;
using CompanySearch.Infrastructure.Persistence;
using CompanySearch.Infrastructure.Persistence.Repositories;
using CompanySearch.Infrastructure.Websites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CompanySearch.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenStreetMapOptions>(configuration.GetSection(OpenStreetMapOptions.SectionName));
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.Configure<EmailDeliveryOptions>(configuration.GetSection(EmailDeliveryOptions.SectionName));
        services.Configure<WebsiteAuditOptions>(configuration.GetSection(WebsiteAuditOptions.SectionName));

        var useSqliteForLocalDevelopment = configuration.GetValue("Persistence:UseSqliteForLocalDevelopment", false);
        var useInMemoryCache = configuration.GetValue("Caching:UseInMemoryCache", false);
        var useInlineScheduler = configuration.GetValue("Jobs:UseInlineScheduler", false);
        var postgresConnectionString = configuration.GetConnectionString("PostgreSql")
                                       ?? "Host=localhost;Port=5432;Database=companysearch;Username=postgres;Password=postgres";
        var sqliteConnectionString = configuration.GetConnectionString("Sqlite")
                                     ?? "Data Source=companysearch-dev.db";

        var redisConnectionString = configuration.GetConnectionString("Redis")
                                    ?? "localhost:6379";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (useSqliteForLocalDevelopment)
            {
                options.UseSqlite(sqliteConnectionString);
            }
            else
            {
                options.UseNpgsql(postgresConnectionString);
            }
        });

        if (useInMemoryCache)
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "companysearch:";
            });
        }

        services.AddHttpClient(OpenStreetMapGeocodingService.NominatimHttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenStreetMapOptions>>().Value;
            client.BaseAddress = new Uri(
                string.IsNullOrWhiteSpace(opts.NominatimBaseUrl)
                    ? "https://nominatim.openstreetmap.org/"
                    : opts.NominatimBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.Clear();
            var ua = string.IsNullOrWhiteSpace(opts.UserAgent)
                ? "CompanySearch/1.0 (set OpenStreetMap:UserAgent in appsettings)"
                : opts.UserAgent.Trim();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddHttpClient(OpenStreetMapGeocodingService.PhotonHttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<OpenStreetMapOptions>>().Value;
            client.BaseAddress = new Uri(
                string.IsNullOrWhiteSpace(opts.PhotonBaseUrl)
                    ? "https://photon.komoot.io/"
                    : opts.PhotonBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.Clear();
            var ua = string.IsNullOrWhiteSpace(opts.UserAgent)
                ? "CompanySearch/1.0 (set OpenStreetMap:UserAgent in appsettings)"
                : opts.UserAgent.Trim();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }).AddStandardResilienceHandler();

        services.AddHttpClient(OpenStreetMapBusinessDiscoveryService.HttpClientName, (_, client) =>
        {
            client.BaseAddress = new Uri(configuration["OpenStreetMap:OverpassBaseUrl"] ?? "https://overpass-api.de/api/");
            client.Timeout = TimeSpan.FromSeconds(45);
        }).AddStandardResilienceHandler();

        services.AddHttpClient(WebsiteCrawlerService.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        }).AddStandardResilienceHandler();

        services.AddHttpClient(OpenAiEmailComposerService.HttpClientName, (_, client) =>
        {
            client.BaseAddress = new Uri(configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddStandardResilienceHandler();

        services.AddScoped<ApplicationDbContext>();
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IApplicationDataPurge, ApplicationDataPurgeService>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IWebsiteAnalysisRepository, WebsiteAnalysisRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<ISearchJobRepository, SearchJobRepository>();

        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAppCache, DistributedAppCache>();
        services.AddScoped<IGeocodingService, OpenStreetMapGeocodingService>();
        services.AddScoped<IBusinessDiscoveryService, OpenStreetMapBusinessDiscoveryService>();
        services.AddScoped<IWebsiteCrawlerService, WebsiteCrawlerService>();
        services.AddScoped<IEmailComposerService, OpenAiEmailComposerService>();
        services.AddScoped<IEmailSenderService, SmtpEmailSenderService>();
        services.AddScoped<BackgroundJobDispatcher>();
        services.AddScoped<DatabaseSeeder>();

        if (useInlineScheduler)
        {
            services.AddSingleton<IJobScheduler, InlineJobScheduler>();
        }
        else
        {
            services.AddScoped<IJobScheduler, HangfireJobScheduler>();
        }

        return services;
    }
}
