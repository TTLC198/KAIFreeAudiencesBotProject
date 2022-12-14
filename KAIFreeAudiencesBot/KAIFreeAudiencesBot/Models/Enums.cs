using System.ComponentModel;

namespace KAIFreeAudiencesBot.Models;

public enum Parity
{
    [Description("Четная неделя")]
    Even,
    [Description("Четная неделя")]
    NotEven,
}

public enum Modes
{
    [Description("Режим по умолчанию")]
    Default,
    [Description("Просмотр свободных аудиторий в определенную дату")]
    SpecificDates,
    [Description("Просмотр всех свободных аудиторий в определенную дату")]
    SpecificDatesAllAudiences,
    [Description("Просмотр свободных аудиторий по дням недели")]
    SpecificDaysOfWeek,
    [Description("Просмотр всех свободных аудиторий в здании")]
    SpecificDaysAllAudiences,
}

public enum Buildings
{
    [Description("Все здания")]
    All,
    [Description("Первое здание")]
    First,
    [Description("Второе здание")]
    Second, 
    [Description("Третье здание")]
    Third,
    [Description("Четвертое здание")]
    Fourth,
    [Description("Пятое здание")]
    Fifth,
    [Description("Шестое здание")]
    Sixth,
    [Description("Седьмое здание")]
    Seventh,
    [Description("Восьмое здание")]
    Eighth
}

public enum ClientSteps
{
    [Description("Первый этап")]
    Default,
    [Description("Выбор четности недели")]
    ChooseParity,
    [Description("Выбор дат")]
    ChooseDates,
    [Description("Выбор здания")]
    ChooseBuilding,
    [Description("Выбор здания при показе всех аудиторий")]
    ChooseBuildingAllAudiences,
    [Description("Выбор дня недели")]
    ChooseDayOfWeek,
    [Description("Выбор времени занятия")]
    ChooseTime,
    [Description("Повторный выбор времени")]
    ChooseCorrectTime,
    [Description("Выбор аудитории")]
    ChooseAudience,
}

public enum DayOfWeekCustom
{
    [Description("Воскресенье")]
    Sunday = 0,
    [Description("Понедельник")]
    Monday = 1,
    [Description("Вторник")]
    Tuesday = 2,
    [Description("Среда")]
    Wednesday = 3,
    [Description("Четверг")]
    Thursday = 4,
    [Description("Пятница")]
    Friday = 5,
    [Description("Суббота")]
    Saturday = 6,
}