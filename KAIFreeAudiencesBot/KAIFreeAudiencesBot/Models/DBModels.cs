using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KAIFreeAudiencesBot.Models;

[Table("class_types")]
public class ClassType
{
    [Column("ct_id")]
    public int id { get; set; }
    [Column("ct_name")]
    public string name { get; set; }
}

[Table("classrooms")]
public class Classroom
{
    [Column("cr_id")]
    public int id { get; set; }
    [Column("cr_name")]
    public string name { get; set; }
    [Column("cr_building")]
    public string building { get; set; }
}

[Table("groups")]
public class Group
{
    [Column("g_id")]
    public int id { get; set; }
    [Column("g_name")]
    public string name { get; set; }
}

[Table("schedule_subject_dates")]
public class ScheduleSubjectDate
{
    [Column("ssd_id")]
    public int id { get; set; }
    
    [Column("ssd_ti_id")]
    [ForeignKey("ssd_ti_id")]
    public int timeIntervalId { get; set; }
    public TimeInterval TimeInterval { get; set; }
    
    [Column("ssd_t_id")]
    [ForeignKey("ssd_t_id")]
    public int teacherId { get; set; }
    public Teacher Teacher { get; set; }
    
    [Column("ssd_cr_id")]
    [ForeignKey("ssd_cr_id")]
    public int classroomId { get; set; }
    public Classroom Classroom { get; set; }
    
    [Column("ssd_ct_id")]
    [ForeignKey("ssd_ct_id")]
    public int classTypeId { get; set; }
    public ClassType ClassType { get; set; }
    
    [Column("ssd_g_id")]
    [ForeignKey("ssd_g_id")]
    public int groupId { get; set; }
    public Group Group { get; set; }
    
    [Column("ssd_date")]
    public DateOnly date { get; set; }
}

[Table("teachers")]
public class Teacher
{
    [Column("t_id")]
    public int id { get; set; }
    [Column("t_name")]
    public string name { get; set; }
}

[Table("time_intervals")]
public class TimeInterval
{
    [Column("ti_id")]
    public int id { get; set; }
    [Column("ti_start")]
    public TimeSpan start { get; set; }
    [Column("ti_end")]
    public TimeSpan end { get; set; }
}

[Table("default_values")]
public class DefaultValues
{
    [Column("values")]
    public DateOnly values { get; set; }
}