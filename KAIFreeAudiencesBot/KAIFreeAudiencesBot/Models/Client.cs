namespace KAIFreeAudiencesBot.Services.Misc;

public class Client
{
    public long id { get; set; }
    public ClientSteps step { get; set; }
    public ClientSettings settings { get; set; } = new ClientSettings();
}

public class ClientSettings
{
    public string Audience { get; set; }
    public Parity Parity { get; set; }
    
    public Buildings Building { get; set; }
    public Days Day { get; set; }
    public TimeOnly StartTime { get; set; }
    public DateTime DateTime { get; set; }
    public Modes Mode { get; set; }
}

public enum Parity
{
    Even,
    NotEven
}

public enum Modes
{
    Auto,
    Manual
}

public enum Buildings
{
    All,
    First,
    Second, 
    Third,
    Fourth,
    Fifth,
    Sixth,
    Seventh,
    Eighth
}

public enum ClientSteps
{
    Default,
    ChooseParity,
    ChooseBuilding,
    ChooseDay,
    ChooseTime,
    ChooseCorrectTime,
    ChooseAudience,
}

public enum Days
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday
}