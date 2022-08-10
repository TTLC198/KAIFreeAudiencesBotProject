namespace KAIFreeAudiencesBot.Models;

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