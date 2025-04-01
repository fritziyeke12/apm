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

        public static void LogTransaction(string name, string type)
        {
            var transaction = Agent.Tracer.StartTransaction(name, type);
            try
            {
                // Capture additional metadata manually
                var transactionData = new
                {
                    Name = transaction.Name,
                    Type = transaction.Type,
                    StartTime = transaction.Timestamp, // Store original event time
                    ProcessId = Process.GetCurrentProcess().Id,
                    MachineName = Environment.MachineName,
                    DotNetVersion = Environment.Version.ToString(),
                    Labels = new Dictionary<string, object>
                    {
                        { "OfflineQueued", !IsInternetAvailable() }
                    }
                };

                if (IsInternetAvailable())
                {
                    transaction.SetLabel("OfflineQueued", false);
                    transaction.End();
                }
                else
                {
                    SaveToLocalQueue(transactionData);
                }
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

        private static void SaveToLocalQueue(object transactionData)
        {
            List<object> transactions = new List<object>();

            if (File.Exists(LogFilePath))
            {
                var existingData = File.ReadAllText(LogFilePath);
                transactions = JsonSerializer.Deserialize<List<object>>(existingData) ?? new List<object>();
            }

            transactions.Add(transactionData);
            File.WriteAllText(LogFilePath, JsonSerializer.Serialize(transactions));
        }

        public static async Task RetrySendingQueuedTransactions()
        {
            if (!IsInternetAvailable() || !File.Exists(LogFilePath)) return;

            var existingData = File.ReadAllText(LogFilePath);
            var transactions = JsonSerializer.Deserialize<List<dynamic>>(existingData) ?? new List<dynamic>();

            foreach (var transactionData in transactions)
            {
                var transaction = Agent.Tracer.StartTransaction(transactionData.Name, transactionData.Type);
                transaction.Timestamp = transactionData.StartTime; // Set the original timestamp
                transaction.SetLabel("OfflineQueued", true);
                transaction.SetLabel("RestoredProcessId", transactionData.ProcessId);
                transaction.SetLabel("RestoredMachine", transactionData.MachineName);
                transaction.SetLabel("RestoredDotNet", transactionData.DotNetVersion);
                transaction.End();
            }

            File.Delete(LogFilePath); // Clear the queue after sending
        }
    }
}
