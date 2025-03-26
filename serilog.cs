using System;
using System.Windows;
using Serilog;
using Serilog.Sinks.Elasticsearch;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Configure Serilog to write to Elasticsearch.
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("https://your-apm-endpoint.example.com"))
            {
                AutoRegisterTemplate = true,
                // Optionally set an index format, e.g., "wpf-logs-{0:yyyy.MM}"
                IndexFormat = "wpf-logs-{0:yyyy.MM}"
            })
            .WriteTo.Console() // Optional: also log to console for debugging.
            .CreateLogger();

        DateTime startupTime = DateTime.UtcNow;
        Log.Information("Application started at {StartupTime}", startupTime);

        base.OnStartup(e);
    }
}
