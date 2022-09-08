﻿using System.Drawing;
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
    
    public static List<DateOnly> GetDates(List<DaysOfWeek> daysOfWeeks, DateOnly? starts, DateOnly? ends, List<Parity> parities)
    {
        var dates = new List<DateOnly>();
        var start = starts ?? DateOnly.MaxValue;// HEEEELP
        var end = ends ?? DateOnly.MaxValue;// HEEEELP
        foreach (var parity in parities)
        {
            while ((DaysOfWeek)(Enum.Parse(typeof(DaysOfWeek), start.DayOfWeek.ToString())) != daysOfWeeks[0] ||
                   GetWeekParity(start.ToDateTime(TimeOnly.MinValue)) != parity)
            {
                start.AddDays(1);
            }

            dates.Add(start);
        }

        start = dates.Min();
        var resultDates = new List<DateOnly>();
        while (start <= end)
        {
            if (daysOfWeeks.Contains((DaysOfWeek)(Enum.Parse(typeof(DaysOfWeek), start.DayOfWeek.ToString()))) &&
                parities.Contains(GetWeekParity(start.ToDateTime(TimeOnly.MinValue))))
            {
                resultDates.Add(start);
            }

            start.AddDays(1);
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
        var i = 0;
        var j = 0;
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
                    Enum.GetValues(typeof(DaysOfWeek)).Cast<DaysOfWeek>().ToList()[
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
                var days = Enum.GetValues(typeof(DaysOfWeek)).Cast<DaysOfWeek>().ToList();
                var buttons = keyboard.InlineKeyboard!.ToArray();
                for (var i = 0; i < buttons.Length - 1; i++)
                {
                    for (var j = 0; j < buttons[i].ToArray().Length; j++)
                    {
                        keyboard.InlineKeyboard.ToArray()[i].ToArray()[j].Text =
                            clientSettings.DaysOfWeek.IndexOf(days[i * 3 + j]) != -1
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
}