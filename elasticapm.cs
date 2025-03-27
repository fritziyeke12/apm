using System;
using System.Windows;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.Config;

namespace YourNamespace
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set configuration via environment variables.
            Environment.SetEnvironmentVariable("ELASTIC_APM_SERVER_URL", "https://your-apm-endpoint.example.com");
            Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_NAME", "YourWpfApp");
            // Optionally set additional settings:
            // Environment.SetEnvironmentVariable("ELASTIC_APM_ENVIRONMENT", "production");

            // Initialize the Elastic APM agent by passing a new AgentComponents instance.
            Agent.Setup(new AgentComponents());

            // Start a transaction representing the application startup.
            var transaction = Agent.Tracer.StartTransaction("ApplicationStartup", "startup");
            try
            {
                // Optionally add custom labels or context.
                transaction.SetLabel("StartupTime", DateTime.UtcNow.ToString("o"));

                // Execute your startup logic here.
                // For example, initializing resources, loading configuration, etc.
            }
            catch (Exception ex)
            {
                // Capture any exceptions that occur during startup.
                transaction.CaptureException(ex);
                throw;
            }
            finally
            {
                // End the transaction so that the agent sends the data to the APM server.
                transaction.End();
            }

            base.OnStartup(e);
        }
    }
}
