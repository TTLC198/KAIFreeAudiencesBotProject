namespace KAIFreeAudiencesBot.Models;

public class Client
{
    public long id { get; set; }
    public ClientSteps step { get; set; }
    public ClientSettings settings { get; set; } = new ClientSettings();
}

public class ClientSettings
{
    public List<string> Audience { get; set; } = new List<string>();
    public List<Parity> Parity { get; set; } = new List<Parity>();
    
    public Buildings Building { get; set; }
    public List<DaysOfWeek> DaysOfWeek { get; set; } = new List<DaysOfWeek>();
    public TimeOnly TimeStart { get; set; }
    public DateOnly? DateStart { get; set; }
    public DateOnly? DateEnd { get; set; }
    public Modes Mode { get; set; }
}