using KAIFreeAudiencesBot.Models;
using System.Globalization;

namespace KAIFreeAudiencesBot.Services;

public static class Misc
{
    public static Parity GetWeekParity(DateTime? time)
    {
        var myCI = new CultureInfo("ru-RU");
        var myCal = myCI.Calendar;
        return myCal.GetWeekOfYear(time ??= DateTime.Now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday) % 2 == 0
            ? Parity.Even
            : Parity.NotEven;
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