using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using KAIFreeAudiencesBot.Models;
using KAIScheduler;

namespace KAIFreeAudiencesBot.Services;

public class ScheduleParser
{
    private static readonly string kaiUrl = "https://82.202.190.197:443/raspisanie";
    private readonly ILogger<ScheduleParser> _logger;

    public ScheduleParser(ILogger<ScheduleParser> logger)
    {
        _logger = logger;
    }
    
    private class GroupApi
    {
        public int id { get; set; }
        public string group { get; set; }
        public string forma { get; set; }
    }
    
    private async Task<List<string>>? GetGroupsIdAsync(string groupNum)
    {
        try
        {
            using var httpClient = new HttpClient(
                new HttpClientHandler 
                { 
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 1,
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                }, false) { DefaultRequestVersion = new Version(2, 0)};
            string request = kaiUrl 
                             + "?p_p_id=pubStudentSchedule_WAR_publicStudentSchedule10"
                             + "&p_p_lifecycle=2"
                             + "&p_p_resource_id=getGroupsURL"
                             + "&query=" + groupNum;
        
            var response = await httpClient.GetAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"The requested feed returned an error: {response.StatusCode}");

            var responseBodyStream = await response.Content.ReadAsStreamAsync();

            var groups = await JsonSerializer.DeserializeAsync<List<GroupApi>>(responseBodyStream);
            var temp = groups;

            return groups!.Select(gr => gr.id.ToString()).ToList();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.Message);
            throw;
        }
    }
    
    private async Task<List<List<JsonProperties>>>? GetScheduleByIdAsync(string groupId)
    {
        using var httpClient = new HttpClient();
        
        string request = kaiUrl 
                         + "?p_p_id=pubStudentSchedule_WAR_publicStudentSchedule10"
                         + "&p_p_lifecycle=2"
                         + "&p_p_resource_id=schedule"
                         + "&groupId=" + groupId;

        var responseBody = await httpClient.GetStreamAsync(request);
        return await JsonSerializer.DeserializeAsync<List<List<JsonProperties>>>(responseBody);
    }

    public async Task ParseScheduleAsync()
    {
        Dictionary<string, List<List<JsonProperties>>> fullSchedule =
            new Dictionary<string, List<List<JsonProperties>>>();
        for (int i = 1; i < 9; i++)
        {
            var temp1 = await GetGroupsIdAsync($"{i}");
            foreach (var groupId in await GetGroupsIdAsync($"{i}"))
            {
                var schedule = await GetScheduleByIdAsync(groupId);
                if (schedule == null) continue;
                fullSchedule.Add(groupId, schedule);
            }
        }

        var temp = fullSchedule;
    }
}