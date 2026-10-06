using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace FinCore.BuildingBlocks.Observability;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddFinCoreLogging(this WebApplicationBuilder builder, string serviceName)
    {
        var elasticsearchUrl = builder.Configuration["Elasticsearch:Url"];

        builder.Host.UseSerilog((_, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)   // lets Kibana filter by service
                .WriteTo.Console();

            if (!string.IsNullOrWhiteSpace(elasticsearchUrl))
            {
                loggerConfiguration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
                {
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                    IndexFormat = "fincore-logs-{0:yyyy.MM.dd}",
                    NumberOfShards = 1,
                    NumberOfReplicas = 0
                });
            }
        });

        return builder;
    }
}