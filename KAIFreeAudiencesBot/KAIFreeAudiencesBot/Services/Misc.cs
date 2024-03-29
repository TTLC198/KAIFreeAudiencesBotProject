﻿using System.Drawing;
using System.Globalization;
using KAIFreeAudiencesBot.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;
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
    
    public static DateOnly GetDefaultValues(List<Parity>? parities, bool? isEnd, DateOnly? dateOnly)
    {
        dateOnly ??= DateOnly.FromDateTime(DateTime.Now);
        var initialDate = dateOnly.Value.Month is >= 1 and <= 6 
            ? new DateTime(dateOnly.Value.Year, 1, 8) 
            : new DateTime(dateOnly.Value.Year, 9, 1);
        if (isEnd is true)
            return dateOnly.Value.Month is >= 1 and <= 6 
                ? new DateOnly(dateOnly.Value.Year, 6, 30) 
                : new DateOnly(dateOnly.Value.Year, 12, 31);

        if (parities is not null)
            return parities.Any(p => p == GetWeekParity(initialDate)) 
                ? DateOnly.FromDateTime(initialDate - new TimeSpan(days: (int) initialDate.DayOfWeek, 0, 0, 0)) 
                : DateOnly.FromDateTime(initialDate + new TimeSpan(days: 8 - (int) initialDate.DayOfWeek, 0, 0, 0));

        return DateOnly.FromDateTime(initialDate);

    }
    
    public static string GetDescription(this Enum genericEnum)
    {
        var genericEnumType = genericEnum.GetType();
        if (genericEnumType.GetMember(genericEnum.ToString()) is not {Length: > 0} memberInfo)
            return genericEnum.ToString();
        if ((memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false) is { } attribs && attribs.Any()))
        {
            return ((System.ComponentModel.DescriptionAttribute)attribs.ElementAt(0)).Description;
        }
        return genericEnum.ToString();
    }
    
    public static List<DateOnly> GetDates(List<DayOfWeekCustom> daysOfWeeks, DateOnly? starts, DateOnly? ends, List<Parity> parities)
    {
        var dates = new List<DateOnly>();
        var start = starts ?? DateOnly.MaxValue;// HEEEELP
        var end = ends ?? DateOnly.MaxValue;// HEEEELP
        foreach (var parity in parities)
        {
            while ((DayOfWeekCustom)(Enum.Parse(typeof(DayOfWeekCustom), start.DayOfWeek.ToString())) != daysOfWeeks[0] ||
                   GetWeekParity(start.ToDateTime(TimeOnly.MinValue)) != parity)
            {
                var temp = (DayOfWeek) (Enum.Parse(typeof(DayOfWeek), start.DayOfWeek.ToString()));
                start = start.AddDays(1);
            }

            dates.Add(start);
        }

        start = dates.Min();
        var resultDates = new List<DateOnly>();
        while (start <= end)
        {
            if (daysOfWeeks.Contains((DayOfWeekCustom)(Enum.Parse(typeof(DayOfWeekCustom), start.DayOfWeek.ToString()))) &&
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
            case ClientSteps.ChooseDayOfWeek:
                var dayOfWeek =
                    Enum.GetValues(typeof(DayOfWeekCustom)).Cast<DayOfWeekCustom>().ToList()[
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
            case ClientSteps.ChooseDayOfWeek:
                var days = Enum.GetValues(typeof(DayOfWeekCustom)).Cast<DayOfWeekCustom>().ToList();
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

    public static async Task<Stream?> HtmlToImageStreamConverter(string html, Size imageSize)
    {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        await using var browser = await Puppeteer.LaunchAsync(
            new LaunchOptions { Headless = true,
                Args = new [] {
                    "--disable-gpu",
                    "--disable-dev-shm-usage",
                    "--disable-setuid-sandbox",
                    "--no-sandbox"}});
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        await page.SetViewportAsync(new ViewPortOptions
        {
            Height = imageSize.Height,
            Width = imageSize.Width
        });
        return await page.ScreenshotStreamAsync(new ScreenshotOptions
        {
            Clip = new Clip
            {
                Height = imageSize.Height,
                Width = imageSize.Width
            }
        });
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
                if (!lastSchedule.dates.Contains(schedule.date))
                {
                    resultSchedule.Add(new ValueTuple<string, string, List<DateOnly>>(schedule.Classroom.name, schedule.Classroom.building,
                        new List<DateOnly> { schedule.date, schedule.date }));
                }
            }
        }

        return resultSchedule.Select(rs =>
            (
                rs.audience,
                rs.building,
                rs.dates.First().DayOfYear - rs.dates.Last().DayOfYear == 0 ? rs.dates.Select(r => r.ToString(new CultureInfo("ru-RU"))).First() : string.Join('-', rs.dates.Select(r => r.ToString(new CultureInfo("ru-RU")))),
                schedules[0].TimeInterval.start.ToString("HH:mm")
                )).ToList();
    }
}