using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YourNamespace; // Replace with the namespace where ApmLogRetryWorker is defined

public class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
            .UseWindowsService() // Configures the host to run as a Windows Service
            .ConfigureServices(services =>
            {
                // Register the background worker that handles queued logs.
                services.AddHostedService<ApmLogRetryWorker>();
            })
            .Build();

        await host.RunAsync();
    }
}
