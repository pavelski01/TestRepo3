using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;

class Program
{
    static readonly Meter s_meter = new("GeneratorMeter", "1.0.0");
    static readonly Counter<int> s_numbersGenerated = s_meter.CreateCounter<int>(
        name: "number-generated",
        //unit: "numbers",
        description: "Numbers generated");

    static void Main(string[] args)
    {
        using var channel = new InMemoryChannel();

        IServiceCollection services = new ServiceCollection();
        services.Configure<TelemetryConfiguration>(config => config.TelemetryChannel = channel);
        services.AddLogging(builder =>
        {
            // Only Application Insights is registered as a logger provider
            builder.AddApplicationInsights(
                configureTelemetryConfiguration: (config) => config.ConnectionString = "InstrumentationKey=c469665d-0c16-497e-9a55-26971a7e950a;IngestionEndpoint=https://polandcentral-0.in.applicationinsights.azure.com/;LiveEndpoint=https://polandcentral.livediagnostics.monitor.azure.com/",
                configureApplicationInsightsLoggerOptions: (options) => { }
            );
        });

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("GeneratorMeter")
                .AddPrometheusHttpListener(options => options.UriPrefixes = new string[] { "http://host.docker.internal:9184/" })
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(opts => opts.Endpoint = new Uri("http://host.docker.internal:4317"))
                .Build();

        var rand = Random.Shared;
        Console.WriteLine("Press any key to exit");
        int number;
        while (!Console.KeyAvailable)
        {
            number = rand.Next(100, 1000);
            Thread.Sleep(number);
            s_numbersGenerated.Add(number);
            logger.LogInformation("Logger is working...");
        }
    }
}