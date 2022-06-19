using KAIFreeAudiencesBot.Models;
using Microsoft.EntityFrameworkCore;

namespace KAIFreeAudiencesBot.Services.Database;

public sealed class SchDbContext : DbContext
{
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupsWeekDay> GroupsWeekDays { get; set; }
    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<TimeRange> TimeRanges { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Lesson> Lessons { get; set; }

    public SchDbContext()
    {
        Database.EnsureCreated();
    }
}