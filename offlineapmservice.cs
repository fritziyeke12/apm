using System;
using System.ServiceProcess;
using System.Timers;
using System.Threading.Tasks;
using YourNamespace; // Ensure this points to where ApmOfflineLogger is defined

namespace ApmLogService
{
    public class ApmLogRetryService : ServiceBase
    {
        private Timer _timer;
        private const double IntervalInMilliseconds = 10 * 60 * 1000; // Every 10 minutes

        public ApmLogRetryService()
        {
            ServiceName = "ApmLogRetryService";
        }

        protected override void OnStart(string[] args)
        {
            // Set up a timer to run the retry logic at the defined interval.
            _timer = new Timer(IntervalInMilliseconds);
            _timer.Elapsed += async (sender, eventArgs) => await ProcessQueuedLogs();
            _timer.AutoReset = true;
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        // Method that invokes the retry logic.
        private async Task ProcessQueuedLogs()
        {
            try
            {
                await ApmOfflineLogger.RetrySendingQueuedTransactions();
            }
            catch (Exception ex)
            {
                // Optionally log errors to the Event Log or a local file.
            }
        }

        // Main entry point when running as a service.
        public static void Main()
        {
            ServiceBase.Run(new ApmLogRetryService());
        }
    }
}
