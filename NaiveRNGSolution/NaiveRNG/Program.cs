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
        }
    }
}