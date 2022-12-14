using System.Globalization;
using KAIFreeAudiencesBot.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace KAIFreeAudiencesBot.Models;

public static class Keyboard
{
    public static readonly InlineKeyboardMarkup InlineModeKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Дни недели", callbackData: "0_days"),
            InlineKeyboardButton.WithCallbackData(text: "Дата", callbackData: "0_dates"),
        }
    });

    public static readonly InlineKeyboardMarkup InlineWeekKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "☑ Чет", callbackData: "1_e"),
            InlineKeyboardButton.WithCallbackData(text: "☑ Нечет", callbackData: "1_n"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Текущая четность", callbackData: $"3_{(Misc.GetWeekParity(DateTime.Today) == Parity.Even ? 'e' : 'n')}"),
        },
        new [] {
            InlineKeyboardButton.WithCallbackData(text: "Подтвердить", callbackData: "3_submit")
        }
    });

    public static readonly InlineKeyboardMarkup InlineDayKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "☑ Пн", callbackData: "1_1"),
            InlineKeyboardButton.WithCallbackData(text: "☑ Вт", callbackData: "1_2"),
            InlineKeyboardButton.WithCallbackData(text: "☑ Ср", callbackData: "1_3")
            
        },
        new[] {
            InlineKeyboardButton.WithCallbackData(text: "☑ Чт", callbackData: "1_4"),
            InlineKeyboardButton.WithCallbackData(text: "☑ Пт", callbackData: "1_5"),
            InlineKeyboardButton.WithCallbackData(text: "☑ Сб", callbackData: "1_6")
        },
        new []
        {
            InlineKeyboardButton.WithCallbackData(text:"Подтвердить", callbackData: "4_submit")
        }
    });

    public static readonly InlineKeyboardMarkup InlineTimeKeyboard = new(new[]
    {
        new[] {InlineKeyboardButton.WithCallbackData(text: "8:00 - 9:30", callbackData: "5_8:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "8:15 - 9:45", callbackData: "5_8:15")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "9:40 - 11:10", callbackData: "5_9:40")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "9:55 - 11:25", callbackData: "5_9:55")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "11:20 - 12:50", callbackData: "5_11:20")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "11:35 - 13:05", callbackData: "5_11:35")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "12:10 - 13:40", callbackData: "5_12:10")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "13:00 - 14:30", callbackData: "5_13:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "13:30 - 15:00", callbackData: "5_13:30")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "13:50 - 15:20", callbackData: "5_13:50")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "15:10 - 16:40", callbackData: "5_15:10")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "15:30 - 17:00", callbackData: "5_15:30")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "16:50 - 18:20", callbackData: "5_16:50")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "17:05 - 18:35", callbackData: "5_17:05")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "17:10 - 18:40", callbackData: "5_17:10")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:00 - 19:30", callbackData: "5_18:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:25 - 19:55", callbackData: "5_18:25")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:30 - 20:00", callbackData: "5_18:30")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:45 - 20:15", callbackData: "5_18:45")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "19:40 - 21:10", callbackData: "5_19:40")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "20:00 - 21:30", callbackData: "5_20:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "20:05 - 21:35", callbackData: "5_20:05")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "21:40 - 23:10", callbackData: "5_21:40")}
    });

    public static readonly InlineKeyboardMarkup InlineAutoBuildingKeyboard = new(
        new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "1", callbackData: "6_1_auto"),
                InlineKeyboardButton.WithCallbackData(text: "2", callbackData: "6_2_auto"),
                InlineKeyboardButton.WithCallbackData(text: "3", callbackData: "6_3_auto"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "4", callbackData: "6_4_auto"),
                InlineKeyboardButton.WithCallbackData(text: "5", callbackData: "6_5_auto"),
                InlineKeyboardButton.WithCallbackData(text: "6", callbackData: "6_6_auto"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "7", callbackData: "6_7_auto"),
                InlineKeyboardButton.WithCallbackData(text: "8", callbackData: "6_8_auto"),
                InlineKeyboardButton.WithCallbackData(text: "Все", callbackData: "6_all"),
            }
        });
    
    public static readonly InlineKeyboardMarkup InlineBuildingKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "1", callbackData: "7_1"),
            InlineKeyboardButton.WithCallbackData(text: "2", callbackData: "7_2"),
            InlineKeyboardButton.WithCallbackData(text: "3", callbackData: "7_3"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "4", callbackData: "7_4"),
            InlineKeyboardButton.WithCallbackData(text: "5", callbackData: "7_5"),
            InlineKeyboardButton.WithCallbackData(text: "6", callbackData: "7_6"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "7", callbackData: "7_7"),
            InlineKeyboardButton.WithCallbackData(text: "8", callbackData: "7_8"),
            InlineKeyboardButton.WithCallbackData(text: "Все", callbackData: "7_all"),
        }
    });

    public static readonly InlineKeyboardMarkup InlineAllAudiences = new(new[]
    {
        InlineKeyboardButton.WithCallbackData(text: "Показать все свободные аудитории", callbackData: "6_all"),
    });

    public static InlineKeyboardMarkup InlineYnRoomKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Да", callbackData: "7_y"),
            InlineKeyboardButton.WithCallbackData(text: "Нет", callbackData: "7_n")
        }
    });

    public static readonly InlineKeyboardMarkup InlineRestartKeyboard = new(new[]
    {
        InlineKeyboardButton.WithCallbackData("Повторить", "8_y"),
        InlineKeyboardButton.WithCallbackData("В начало", "8_n")
    });
    
    public static readonly InlineKeyboardMarkup Back = new(new[]
    {
        InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "b")
    });

    public static InlineKeyboardMarkup BuildCalendar(string[] args)
    {
        var currentDate = DateTime.Today;
        InlineKeyboardMarkup keyboardMarkup = null!;

        switch (args[1])
        {
            case "r":
                if (args.Length != 4)
                {
                    currentDate = DateTime.ParseExact(args[2], "dd.MM.yy", CultureInfo.CreateSpecificCulture("ru")).AddMonths(1);
                    goto default;
                }
                else
                {
                    currentDate =  DateTime.ParseExact(args[2], "dd.MM.yy", CultureInfo.CreateSpecificCulture("ru")).AddYears(1);
                    goto case "month";
                }
            case "l":
                if (args.Length != 4)
                {
                    currentDate = DateTime.ParseExact(args[2], "dd.MM.yy", CultureInfo.CreateSpecificCulture("ru")).AddMonths(-1);
                    goto default;
                }
                else
                {
                    currentDate = DateTime.ParseExact(args[2], "dd.MM.yy", CultureInfo.CreateSpecificCulture("ru")).AddYears(-1);
                    goto case "month";
                }
            case "null":
                goto default;
            case "jan":
                currentDate = new DateTime(currentDate.Year, 1, currentDate.Day);
                goto default;
            case "feb":
                currentDate = new DateTime(currentDate.Year, 2, currentDate.Day);
                goto default;
            case "mar":
                currentDate = new DateTime(currentDate.Year, 3, currentDate.Day);
                goto default;
            case "apr":
                currentDate = new DateTime(currentDate.Year, 4, currentDate.Day);
                goto default;
            case "may":
                currentDate = new DateTime(currentDate.Year, 5, currentDate.Day);
                goto default;
            case "jun":
                currentDate = new DateTime(currentDate.Year, 6, currentDate.Day);
                goto default;
            case "jul":
                currentDate = new DateTime(currentDate.Year, 7, currentDate.Day);
                goto default;
            case "aug":
                currentDate = new DateTime(currentDate.Year, 8, currentDate.Day);
                goto default;
            case "sep":
                currentDate = new DateTime(currentDate.Year, 9, currentDate.Day);
                goto default;
            case "oct":
                currentDate = new DateTime(currentDate.Year, 10, currentDate.Day);
                goto default;
            case "nov":
                currentDate = new DateTime(currentDate.Year, 11, currentDate.Day);
                goto default;
            case "dec":
                currentDate = new DateTime(currentDate.Year, 12, currentDate.Day);
                goto default;
            case "month":
                keyboardMarkup = new InlineKeyboardMarkup(new []
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Январь", "2_jan"),
                        InlineKeyboardButton.WithCallbackData("Февраль", "2_feb"),
                        InlineKeyboardButton.WithCallbackData("Март", "2_mar"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Апрель", "2_apr"),
                        InlineKeyboardButton.WithCallbackData("Май", "2_may"),
                        InlineKeyboardButton.WithCallbackData("Июнь", "2_jun"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Июль", "2_jul"),
                        InlineKeyboardButton.WithCallbackData("Август", "2_aug"),
                        InlineKeyboardButton.WithCallbackData("Сентябрь", "2_sep"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Октябрь", "2_oct"),
                        InlineKeyboardButton.WithCallbackData("Ноябрь", "2_nov"),
                        InlineKeyboardButton.WithCallbackData("Декабрь", "2_dec"),
                    },
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("<<", $"2_l_{currentDate.ToString("dd.MM.yy", CultureInfo.CreateSpecificCulture("ru"))}_y"), 
                        InlineKeyboardButton.WithCallbackData($"{currentDate.Year}", "2_null"), 
                        InlineKeyboardButton.WithCallbackData(">>", $"2_r_{currentDate.ToString("dd.MM.yy", CultureInfo.CreateSpecificCulture("ru"))}_y"), 
                    }
                });
                break;
            default:
                List<List<InlineKeyboardButton>> markup = new List<List<InlineKeyboardButton>>()
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Вс", "2_null"),
                        InlineKeyboardButton.WithCallbackData("Пн", "2_null"),
                        InlineKeyboardButton.WithCallbackData("Вт", "2_null"),
                        InlineKeyboardButton.WithCallbackData("Ср", "2_null"),
                        InlineKeyboardButton.WithCallbackData("Чт", "2_null"),
                        InlineKeyboardButton.WithCallbackData("Пт", "2_null"),
                        InlineKeyboardButton.WithCallbackData("Сб", "2_null"),
                    }.ToList(),
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Сегодня", $"4_{DateTime.Today.ToString("dd.MM.yy", CultureInfo.CreateSpecificCulture("ru"))}"),
                    }.ToList(),
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("<<", $"2_l_{currentDate.ToString("dd.MM.yy", CultureInfo.CreateSpecificCulture("ru"))}"), 
                        InlineKeyboardButton.WithCallbackData($"{currentDate.ToString("MMMM", CultureInfo.CreateSpecificCulture("ru"))} {currentDate.Year}", "2_month"), 
                        InlineKeyboardButton.WithCallbackData(">>", $"2_r_{currentDate.ToString("dd.MM.yy", CultureInfo.CreateSpecificCulture("ru"))}"), 
                    }.ToList(),
                };
                
                var startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                int days = DateTime.DaysInMonth(currentDate.Year, currentDate.Month),
                    dayIterator = 0,
                    dayOfWeekIterator = (int) startDate.DayOfWeek;
                var endDate = new DateTime(currentDate.Year, currentDate.Month, days);
                
                for (var i = 0; i < 6; i++)
                {
                    if (dayIterator == (endDate - startDate).Days + 1) break;
                    var tempMarkup = new List<InlineKeyboardButton>();
                    for (var j = 0; j < 7; j++)
                    {
                        if (j == dayOfWeekIterator && dayIterator != (endDate - startDate).Days + 1)
                        {
                            tempMarkup.Add(
                                InlineKeyboardButton.WithCallbackData($"{dayIterator + 1}", $"4_{new DateTime(currentDate.Year, currentDate.Month, dayIterator + 1).ToString("dd.MM.yy", CultureInfo.CreateSpecificCulture("ru"))}")
                            );
                            dayIterator++;
                            dayOfWeekIterator = dayOfWeekIterator == 6 ? 0 : dayOfWeekIterator + 1;
                        }
                        else
                        {
                            tempMarkup.Add(
                                InlineKeyboardButton.WithCallbackData(" ", "2_null")
                            );
                        }
                    }
                    markup.Insert(i + 1, tempMarkup);
                }

                keyboardMarkup = new InlineKeyboardMarkup(markup);
                break;
        }
        return keyboardMarkup ?? Back;
    }

    public static readonly ReplyKeyboardMarkup FirstChoice = new(new[]
    {
        new KeyboardButton[]
        {
            char.ConvertFromUtf32(0x1F193) + " Cвободные аудитории", //FREE
        },
        new KeyboardButton[]
        {
            char.ConvertFromUtf32(0x1F4C5) + " Расписание", // Tear-off Calendar
            char.ConvertFromUtf32(0x1F4D1) + " Четность", // bookmark tabs
        },
        new KeyboardButton[]
        {
        char.ConvertFromUtf32(0x1F3EB) + " Все аудитории", //FREE
        },
        new []
        {
           KeyboardButton.WithWebApp(char.ConvertFromUtf32(0x1F517) + "Перейти на страницу проекта", new WebAppInfo
           {
               Url = "https://github.com/TTLC198/KAIFreeAudiencesBotProject"
           }),  
        }
    })
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = true
    };
    
    public static readonly InlineKeyboardMarkup InlineChangeAudKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Изменить", callbackData: "9_change"),
        }
    });
}