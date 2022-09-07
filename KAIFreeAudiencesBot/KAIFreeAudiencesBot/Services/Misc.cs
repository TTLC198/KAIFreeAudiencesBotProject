using System.Drawing;
using System.Globalization;
using System.Text;
using GrapeCity.Documents.Html;
using KAIFreeAudiencesBot.Models;
using Telegram.Bot.Types.InputFiles;

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

    public static Stream? HtmlToImageStreamConverter(string html)
    {
        return HtmlToImageStreamConverter(html, new Size(460, 1000));
    }
    
    public static Stream? HtmlToImageStreamConverter(string html, Size imageSize)
    {
        Stream? imageStream = new MemoryStream();
        
        using (var re1 = new GcHtmlRenderer(html))
        {
            PngSettings imageSettings = new PngSettings();
            imageSettings.DefaultBackgroundColor = Color.Transparent;
            imageSettings.WindowSize = imageSize;
            imageSettings.FullPage = false;
            re1.RenderToPng(imageStream, imageSettings);
        }

        imageStream.Position = 0;
        return imageStream;
    }
}