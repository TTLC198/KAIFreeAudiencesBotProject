using System.Drawing;
using System.Globalization;
using GrapeCity.Documents.Html;
using KAIFreeAudiencesBot.Models;
using Telegram.Bot.Types.ReplyMarkups;

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

    public static List<DateOnly> GetDates(List<DayOfWeek> daysOfWeeks, DateOnly? starts, DateOnly? ends,
        List<Parity> parities)
    {
        var dates = new List<DateOnly>();
        var start = starts ?? DateOnly.MaxValue; // HEEEELP
        var end = ends ?? DateOnly.MaxValue; // HEEEELP
        foreach (var parity in parities)
        {
            while ((DayOfWeek)(Enum.Parse(typeof(DayOfWeek), start.DayOfWeek.ToString())) != daysOfWeeks[0] ||
                   GetWeekParity(start.ToDateTime(TimeOnly.MinValue)) != parity)
            {
                var temp = (DayOfWeek)(Enum.Parse(typeof(DayOfWeek), start.DayOfWeek.ToString()));
                start = start.AddDays(1);
            }

            dates.Add(start);
        }

        start = dates.Min();
        var resultDates = new List<DateOnly>();
        while (start <= end)
        {
            if (daysOfWeeks.Contains((DayOfWeek)(Enum.Parse(typeof(DayOfWeek), start.DayOfWeek.ToString()))) &&
                parities.Contains(GetWeekParity(start.ToDateTime(TimeOnly.MinValue))))
            {
                resultDates.Add(start);
            }

            start = start.AddDays(1);
        }

        return resultDates;
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

    public static int[] GetIndexes(InlineKeyboardMarkup keyboardMarkup, string match)
    {
        int i = 0, j = 0;

        foreach (var arrayOfButtons in keyboardMarkup!.InlineKeyboard.ToList())
        {
            foreach (var button in arrayOfButtons.ToList())
            {
                if (button.CallbackData == match)
                {
                    return new[] { j, i };
                }

                i++;
            }

            i = 0;
            j++;
        }

        return new[] { -1, -1 };
    }

    public static void ChangeValue(ClientSteps clientStep, ClientSettings clientSettings, InlineKeyboardButton button)
    {
        switch (clientStep)
        {
            case ClientSteps.Default:
                break;
            case ClientSteps.ChooseParity:
                switch (button.CallbackData!.Split('_')[1])
                {
                    case "e":
                        if (clientSettings.Parity.IndexOf(Parity.Even) == -1)
                        {
                            clientSettings.Parity.Add(Parity.Even);
                        }
                        else
                        {
                            clientSettings.Parity.Remove(Parity.Even);
                        }

                        break;
                    case "n":
                        if (clientSettings.Parity.IndexOf(Parity.NotEven) == -1)
                        {
                            clientSettings.Parity.Add(Parity.NotEven);
                        }
                        else
                        {
                            clientSettings.Parity.Remove(Parity.NotEven);
                        }

                        break;
                }

                break;
            case ClientSteps.ChooseDay:
                var dayOfWeek =
                    Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToList()[
                        int.Parse(button.CallbackData!.Split('_')[1])];
                if (clientSettings.DaysOfWeek.IndexOf(dayOfWeek) == -1)
                {
                    clientSettings.DaysOfWeek.Add(dayOfWeek);
                }
                else
                {
                    clientSettings.DaysOfWeek.Remove(dayOfWeek);
                }

                break;
            case ClientSteps.ChooseBuilding:
                break;
            case ClientSteps.ChooseTime:
                break;
            case ClientSteps.ChooseCorrectTime:
                break;
            case ClientSteps.ChooseAudience:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(clientStep), clientStep, null);
        }
    }

    public static InlineKeyboardMarkup UpdateKeyboardMarkup(ClientSteps clientStep, ClientSettings clientSettings,
        InlineKeyboardMarkup keyboard)
    {
        switch (clientStep)
        {
            case ClientSteps.Default:
                break;
            case ClientSteps.ChooseParity:
                keyboard.InlineKeyboard.ToArray()[0].ToArray()[0].Text =
                    clientSettings.Parity.IndexOf(Parity.Even) != -1
                        ? keyboard.InlineKeyboard.ToArray()[0].ToArray()[0].Text.Replace("☑", "✅")
                        : keyboard.InlineKeyboard.ToArray()[0].ToArray()[0].Text.Replace("✅", "☑");
                keyboard.InlineKeyboard.ToArray()[0].ToArray()[1].Text =
                    clientSettings.Parity.IndexOf(Parity.NotEven) != -1
                        ? keyboard.InlineKeyboard.ToArray()[0].ToArray()[1].Text.Replace("☑", "✅")
                        : keyboard.InlineKeyboard.ToArray()[0].ToArray()[1].Text.Replace("✅", "☑");
                break;
            case ClientSteps.ChooseDay:
                var days = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToList();

                var buttons = keyboard.InlineKeyboard!.ToArray();
                for (var i = 0; i < buttons.Length - 1; i++)
                {
                    for (var j = 0; j < buttons[i].ToArray().Length; j++)
                    {
                        keyboard.InlineKeyboard.ToArray()[i].ToArray()[j].Text =
                            clientSettings.DaysOfWeek.IndexOf(days[i * 3 + j + 1]) != -1
                                ? keyboard.InlineKeyboard.ToArray()[i].ToArray()[j].Text.Replace("☑", "✅")
                                : keyboard.InlineKeyboard.ToArray()[i].ToArray()[j].Text.Replace("✅", "☑");
                    }
                }

                break;
            case ClientSteps.ChooseBuilding:
                break;
            case ClientSteps.ChooseTime:
                break;
            case ClientSteps.ChooseCorrectTime:
                break;
            case ClientSteps.ChooseAudience:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(clientStep), clientStep, null);
        }

        return keyboard;
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

    public static List<(string audience, string building, string date, string timeInterval)> SmashDates(
        List<ScheduleSubjectDate> schedules)
    {
        var resultSchedule = new List<(string audience, string building, List<DateOnly> dates)> { };
        foreach (var schedule in schedules)
        {
            if (!resultSchedule.Select(r => (r.building, r.audience))
                    .Contains(new ValueTuple<string, string>(schedule.Classroom.building, schedule.Classroom.name)))
            {
                resultSchedule.Add(new ValueTuple<string, string, List<DateOnly>>(schedule.Classroom.name, schedule.Classroom.building,
                    new List<DateOnly> { schedule.date, schedule.date }));
                continue;
            }

            var lastSchedule = resultSchedule.LastOrDefault(
                rs => 
                    rs.audience == schedule.Classroom.name 
                    && rs.building == schedule.Classroom.building);
            if (lastSchedule.dates[1] == schedule.date.AddDays(schedule.date.DayOfWeek == DayOfWeek.Monday ? -2 : -1))
            {
                lastSchedule.dates[1] = schedule.date;
            }
            else
            {
                resultSchedule.Add(new ValueTuple<string, string, List<DateOnly>>(schedule.Classroom.name, schedule.Classroom.building,
                    new List<DateOnly> { schedule.date, schedule.date }));
            }
        }

        return resultSchedule.Select(rs => (rs.audience, rs.building, string.Join('-', rs.dates), schedules[0].TimeInterval.start.ToString("c"))).ToList();
    }
}