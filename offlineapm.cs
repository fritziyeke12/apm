using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;

namespace YourNamespace
{
    public static class ApmOfflineLogger
    {
        private static readonly string LogFilePath = "apm_offline_logs.json";

        // Check if network is available
        private static bool IsInternetAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        // Log a transaction. If offline, save to local queue.
        public static void LogTransaction(string name, string type)
        {
            // Start a transaction using the Elastic APM agent.
            var transaction = Agent.Tracer.StartTransaction(name, type);
            try
            {
                // Capture additional metadata manually.
                var transactionData = new OfflineTransactionData
                {
                    Name = transaction.Name,
                    Type = transaction.Type,
                    // Capture the original start time (in microseconds since epoch)
                    Timestamp = transaction.Timestamp,
                    ProcessId = Process.GetCurrentProcess().Id,
                    MachineName = Environment.MachineName,
                    DotNetVersion = Environment.Version.ToString(),
                    OfflineQueued = !IsInternetAvailable()
                };

                if (IsInternetAvailable())
                {
                    // Mark this transaction as sent immediately.
                    transaction.SetLabel("OfflineQueued", false);
                    transaction.End();
                }
                else
                {
                    // Save transaction details to the local queue for later sending.
                    SaveToLocalQueue(transactionData);
                    // End the transaction immediately.
                    transaction.End();
                }
            }
            catch (Exception ex)
            {
                // Capture exceptions with the agent.
                transaction.CaptureException(ex);
                transaction.End();
            }
        }

        // Save transaction data to a JSON file
        private static void SaveToLocalQueue(OfflineTransactionData transactionData)
        {
            List<OfflineTransactionData> transactions = new List<OfflineTransactionData>();

            if (File.Exists(LogFilePath))
            {
                try
                {
                    var existingData = File.ReadAllText(LogFilePath);
                    transactions = JsonSerializer.Deserialize<List<OfflineTransactionData>>(existingData) 
                                   ?? new List<OfflineTransactionData>();
                }
                catch
                {
                    // If file is corrupted or unreadable, start fresh.
                    transactions = new List<OfflineTransactionData>();
                }
            }

            transactions.Add(transactionData);
            File.WriteAllText(LogFilePath, JsonSerializer.Serialize(transactions));
        }

        // Retry sending all queued transactions.
        public static async Task RetrySendingQueuedTransactions()
        {
            if (!IsInternetAvailable() || !File.Exists(LogFilePath))
            {
                return;
            }

            var existingData = File.ReadAllText(LogFilePath);
            var transactions = JsonSerializer.Deserialize<List<OfflineTransactionData>>(existingData)
                               ?? new List<OfflineTransactionData>();

            // Process each queued transaction.
            foreach (var data in transactions)
            {
                // Create a new transaction, restoring the original timestamp.
                var transaction = Agent.Tracer.StartTransaction(data.Name, data.Type);
                try
                {
                    // Restore the original timestamp.
                    transaction.Timestamp = data.Timestamp;
                    transaction.SetLabel("OfflineQueued", true);
                    // Restore extra metadata.
                    transaction.SetLabel("RestoredProcessId", data.ProcessId);
                    transaction.SetLabel("RestoredMachine", data.MachineName);
                    transaction.SetLabel("RestoredDotNet", data.DotNetVersion);
                }
                catch (Exception ex)
                {
                    transaction.CaptureException(ex);
                }
                finally
                {
                    transaction.End();
                }
            }

            // Clear the local queue after processing.
            File.Delete(LogFilePath);
            // Optional: simulate asynchronous operation
            await Task.CompletedTask;
        }
    }

    // A helper class that defines the data to be stored offline.
    public class OfflineTransactionData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime Timestamp { get; set; }
        public int ProcessId { get; set; }
        public string MachineName { get; set; }
        public string DotNetVersion { get; set; }
        public bool OfflineQueued { get; set; }
    }
}
