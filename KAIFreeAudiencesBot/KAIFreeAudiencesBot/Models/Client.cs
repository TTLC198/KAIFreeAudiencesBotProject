using KAIFreeAudiencesBot.Models;

namespace KAIFreeAudiencesBot.Services.Models;

public class Client
{
    public long id { get; set; }
    public ClientSteps step { get; set; }
    public ClientSettings settings { get; set; } = new ClientSettings();
}

public class ClientSettings
{
    public string Audience { get; set; }
    public List<Parity> Parity { get; set; } = new List<Parity>();
    
    public Buildings Building { get; set; }
    public List<DaysOfWeek> DaysOfWeek { get; set; } = new List<DaysOfWeek>();
    public TimeOnly TimeStart { get; set; }
    public Modes Mode { get; set; }
}