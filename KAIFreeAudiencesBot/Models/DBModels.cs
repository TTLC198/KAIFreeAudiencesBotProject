using System.Text.Json.Serialization;

namespace KAIFreeAudiencesBot.Models;

public class Group
{
    /// <summary>
    /// id
    /// </summary>
    public int id { get; set; }
    /// <summary>
    /// Номер группы
    /// </summary>
    public int group_number { get; set; }
}

public class GroupsWeekDay : Group
{
    /// <summary>
    /// id
    /// </summary>
    public int id { get; set; }
    /// <summary>
    /// День недели
    /// </summary>
    public int week_day { get; set; }
    /// <summary>
    /// Четность недели
    /// </summary>
    public string parity { get; set; }
    /// <summary>
    /// Номер группы
    /// </summary>
    public int group_id { get; set; }
}

public class Classroom
{
    /// <summary>
    /// id
    /// </summary>
    public int id { get; set; }
    /// <summary>
    /// Номер аудитории
    /// </summary>
    public string classroom_number { get; set; }
    /// <summary>
    /// Учебное здание
    /// </summary>
    public string building { get; set; }
}

public class TimeRange
{
    /// <summary>
    /// id
    /// </summary>
    public int id { get; set; }
    /// <summary>
    /// Время начала занятия
    /// </summary>
    public TimeSpan start_time { get; set; }
    /// <summary>
    /// Время конца занятия
    /// </summary>
    public TimeSpan end_time { get; set; }
}

public class Teacher
{
    /// <summary>
    /// id
    /// </summary>
    public int id { get; set; }
    /// <summary>
    /// ФИО
    /// </summary>
    public string full_name { get; set; }
}

public class Lesson
{
    /// <summary>
    /// id урока
    /// </summary>
    public int id { get; set; }
    /// <summary>
    /// id расписания
    /// </summary>
    public int schedule_id { get; set; }
    /// <summary>
    /// id временного промежутка
    /// </summary>
    public int time_range_id { get; set; }
    /// <summary>
    /// id аудитории
    /// </summary>
    public int classroom_id { get; set; }
    /// <summary>
    /// id учителя
    /// </summary>
    public int teacher_id { get; set; }
}
/// <summary>
/// Общий класс
/// </summary>
public class JsonProperties
{
    [JsonPropertyName("buildNum")] public string building  { get; set; }
    [JsonPropertyName("dayNum")] public int weekDay { get; set; }
    [JsonPropertyName("audNum")] public string classroomNum { get; set; }
    [JsonPropertyName("dayTime")] public string dayTime  { get; set; }
    [JsonPropertyName("prepodName")] public string teacherName  { get; set; }
    [JsonPropertyName("dayDate")] public string dayDate { get; set; }
    [JsonPropertyName("disciplType")] public string lessonType { get; set; }
    [JsonPropertyName("disciplName")] public string lessonName { get; set; }
}