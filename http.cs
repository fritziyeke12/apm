public partial class App : Application
{
    private static readonly HttpClient httpClient = new HttpClient();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DateTime startupTime = DateTime.UtcNow;
        await SendStartupTimeAsync(startupTime);
    }

    private async Task SendStartupTimeAsync(DateTime startupTime)
    {
        // Prepare the payload; adjust the property names as required by your APM.
        var payload = new 
        { 
            eventType = "ApplicationStartup", 
            time = startupTime 
        };

        string jsonData = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        // Replace with your actual Elastic APM endpoint URL.
        string apmEndpoint = "https://your-apm-endpoint.example.com";
        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(apmEndpoint, content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            // Handle/log exceptions as needed.
            System.Diagnostics.Debug.WriteLine("Error sending startup time: " + ex.Message);
        }
    }
}
