using System.Globalization;
using KAIFreeAudiencesBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KAIFreeAudiencesBot.Services.Database;

public sealed class SchDbContext : DbContext
{
    public DbSet<ClassType> classTypes { get; set; }
    public DbSet<Classroom> classrooms { get; set; }
    public DbSet<Group> groups { get; set; }
    public DbSet<ScheduleSubjectDate> scheduleSubjectDates { get; set; }
    public DbSet<Teacher> teachers { get; set; }
    public DbSet<TimeInterval> timeIntervals { get; set; }
    public DbSet<DefaultValues> defaultValues { get; set; }


    public SchDbContext(DbContextOptions<SchDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var converterDate = new ValueConverter<DateOnly, string>(
            v => v.ToString(),
            v => DateOnly.ParseExact(v, "dd.MM.yyyy")
        );

        var converterTime = new ValueConverter<TimeOnly, string>(
            v => v.ToString(),
            v => TimeOnly.ParseExact(v, new[] {"HH:mm", "H:mm"})
        );

        modelBuilder
            .Entity<ScheduleSubjectDate>()
            .Property(ssd => ssd.date)
            .HasConversion(converterDate);
        
        modelBuilder
            .Entity<DefaultValues>()
            .Property(ssd => ssd.value)
            .HasConversion(converterDate);
        
        modelBuilder
            .Entity<TimeInterval>()
            .Property(ti => ti.start)
            .HasConversion(converterTime);
        
        modelBuilder
            .Entity<TimeInterval>()
            .Property(ti => ti.end)
            .HasConversion(converterTime);
    }
}