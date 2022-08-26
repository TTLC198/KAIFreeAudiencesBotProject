using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting.Server;

namespace KAIFreeAudiencesBot.Services;

public class NgrokService
{

    private readonly ILogger<NgrokService> Logger;

    public NgrokService(
        ILogger<NgrokService> logger
    )
    {
        this.Logger = logger;
    }
    
    public async Task<string> GetNgrokPublicUrl()
    {
        using var httpClient = new HttpClient();
        for (var ngrokRetryCount = 0; ngrokRetryCount < 20; ngrokRetryCount++)
        {
            Logger.LogDebug("Get ngrok tunnels attempt: {RetryCount}", ngrokRetryCount + 1);

            try
            {
                var json = await httpClient.GetFromJsonAsync<JsonNode>("http://host.docker.internal:4040/api/tunnels");
                var publicUrl = json["tunnels"].AsArray()
                    .Select(e => e["public_url"].GetValue<string>())
                    .SingleOrDefault(u => u.StartsWith("https://"));
                if (!string.IsNullOrEmpty(publicUrl)) return publicUrl;
            }
            catch
            {
                // ignored
            }

            await Task.Delay(200);
        }

        throw new Exception("Ngrok dashboard did not start in 20 tries");
    }
}
