namespace KAIFreeAudiencesBot.Models;

public class Client
{
    public long id { get; set; }
    public ClientSteps step { get; set; }
    public ClientSettings settings { get; set; } = new ();
}

public class ClientSettings
{
    public List<string> Audience { get; set; } = new ();
    public List<Parity> Parity { get; set; } = new ();
    
    public Buildings Building { get; set; }
    public List<DayOfWeekCustom> DaysOfWeek { get; set; } = new ();
    public TimeOnly TimeStart { get; set; }
    public DateOnly? DateStart { get; set; }
    public DateOnly? DateEnd { get; set; }
    public DateOnly? SpecificDate { get; set; }
    public Modes Mode { get; set; }
}