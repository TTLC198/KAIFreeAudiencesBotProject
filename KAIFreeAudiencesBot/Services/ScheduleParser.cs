using System.Net.Sockets;
using System.Text.Json;
using KAIFreeAudiencesBot.Models;

namespace KAIFreeAudiencesBot.Services;

public class ScheduleParser
{
    private static readonly string kaiUrl = "https://kai.ru/raspisanie";
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
    
    public async Task<List<string>> GetGroupsIdAsync(string groupNum)
    {
        using var httpClient = new HttpClient();
        string request = kaiUrl 
                         + "?p_p_id=pubStudentSchedule_WAR_publicStudentSchedule10"
                         + "&p_p_lifecycle=2"
                         + "&p_p_resource_id=getGroupsURL"
                         + "&query=" + groupNum;

        string responseBody = await (await httpClient.GetAsync(request))
            .EnsureSuccessStatusCode()
            .Content.ReadAsStringAsync();

        await using FileStream fs = new FileStream(responseBody, FileMode.OpenOrCreate);
        var groups = await JsonSerializer.DeserializeAsync<List<GroupApi>>(fs);

        return groups!.Select(gr => gr.id.ToString()).ToList();
    }
    
    public async Task<List<List<JsonProperties>>> GetScheduleByIdAsync(string groupId)
    {
        using var httpClient = new HttpClient();
        string request = kaiUrl 
                         + "?p_p_id=pubStudentSchedule_WAR_publicStudentSchedule10"
                         + "&p_p_lifecycle=2"
                         + "&p_p_resource_id=schedule"
                         + "&groupId=" + groupId;

        string responseBody = await (await httpClient.GetAsync(request))
            .EnsureSuccessStatusCode()
            .Content.ReadAsStringAsync();

        if (responseBody == "{}") return null!;
        
        await using FileStream fs = new FileStream(responseBody, FileMode.OpenOrCreate);
        return await JsonSerializer.DeserializeAsync<List<List<JsonProperties>>>(fs);
    }

    public async Task ParseScheduleAsync()
    {
        Dictionary<string, List<List<JsonProperties>>> fullSchedule =
            new Dictionary<string, List<List<JsonProperties>>>();
        for (int i = 0; i < 9; i++)
        {
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