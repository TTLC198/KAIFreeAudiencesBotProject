namespace KAIFreeAudiencesBot.Models;

public enum Parity
{
    Even,
    NotEven
}

public enum Modes
{
    General,
    GeneralAuto,
    Specific,
    SpecificDais,
    SpecificIntervals
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

public enum DaysOfWeek
{
    Monday = 0,
    Tuesday = 1,
    Wednesday = 2,
    Thursday = 3,
    Friday = 4,
    Saturday = 5,
}