using KAIFreeAudiencesBot.Models;

namespace KAIFreeAudiencesBot.Services;

public static class Misc
{
    public static Parity GetWeekParity(DateTime? time)
    {
        time ??= DateTime.Now;
        var d0 = time.Value.ToUniversalTime().GetTime();
        var d = new DateTime(DateTime.Now.Year, 1, 1);
        var dd = (int) d.DayOfWeek;
        var re = Math.Floor((d0 - d.GetTime()) / 8.64e7) + (dd != 0 ? dd - 1 : 6);
        return (Math.Floor(re / 7) % 2 != 0) ? Parity.NotEven : Parity.Even;
    }
    
    public static DateTime? GetCurrentDay(DateTime? time, Parity parity)
    {
        time ??= DateTime.Now;
        for (int i = 1; i < DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) + 1; i++)
        {
            var tempTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, i);
            if (tempTime.DayOfWeek == time.Value.DayOfWeek && GetWeekParity(tempTime) == parity) return tempTime;
        }

        return null;
    }
    
    private static long GetTime(this DateTime dateTime)
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)((dateTime - epoch).TotalMilliseconds);
    }
}