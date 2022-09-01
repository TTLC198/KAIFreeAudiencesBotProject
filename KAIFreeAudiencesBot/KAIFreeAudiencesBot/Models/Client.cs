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
    public List<Parity> Parity { get; set; }
    
    public Buildings Building { get; set; }
    public Days Day { get; set; }
    public TimeOnly TimeStart { get; set; }
    public Modes Mode { get; set; }
}