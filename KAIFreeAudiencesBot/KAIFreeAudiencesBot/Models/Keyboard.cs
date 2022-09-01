using Telegram.Bot.Types.ReplyMarkups;

namespace KAIFreeAudiencesBot.Models;

public static class Keyboard
{
    public static InlineKeyboardMarkup inlineModeKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Дни недели", callbackData: "0_general"),
            InlineKeyboardButton.WithCallbackData(text: "Дата", callbackData: "0_specific"),
        }
    });

    public static InlineKeyboardMarkup inlineWeekKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "☑ Чет", callbackData: "1_e"),
            InlineKeyboardButton.WithCallbackData(text: "☑ Нечет", callbackData: "1_n"),
        },
        new [] {
            InlineKeyboardButton.WithCallbackData(text: "Submit", callbackData: "2_submit")
        }
    });

    public static InlineKeyboardMarkup inlineDayKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Пн", callbackData: "2_0"),
            InlineKeyboardButton.WithCallbackData(text: "Вт", callbackData: "2_1"),
            InlineKeyboardButton.WithCallbackData(text: "Ср", callbackData: "2_2")
            
        },
        new[] {
            InlineKeyboardButton.WithCallbackData(text: "Чт", callbackData: "2_3"),
            InlineKeyboardButton.WithCallbackData(text: "Пт", callbackData: "2_4"),
            InlineKeyboardButton.WithCallbackData(text: "Сб", callbackData: "2_5")
        },
    });

    public static InlineKeyboardMarkup inlineTimeKeyboard = new(new[]
    {
        new[] {InlineKeyboardButton.WithCallbackData(text: "8:00 - 9:30", callbackData: "3_8:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "8:15 - 9:45", callbackData: "3_8:15")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "9:40 - 11:10", callbackData: "3_9:40")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "9:55 - 11:25", callbackData: "3_9:55")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "11:20 - 12:50", callbackData: "3_11:20")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "11:35 - 13:05", callbackData: "3_11:35")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "12:10 - 13:40", callbackData: "3_12:10")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "13:00 - 14:30", callbackData: "3_13:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "13:30 - 15:00", callbackData: "3_13:30")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "13:50 - 15:20", callbackData: "3_13:50")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "15:10 - 16:40", callbackData: "3_15:10")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "15:30 - 17:00", callbackData: "3_15:30")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "16:50 - 18:20", callbackData: "3_16:50")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "17:05 - 18:35", callbackData: "3_17:05")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "17:10 - 18:40", callbackData: "3_17:10")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:00 - 19:30", callbackData: "3_18:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:25 - 19:55", callbackData: "3_18:25")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:30 - 20:00", callbackData: "3_18:30")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:45 - 20:15", callbackData: "3_18:45")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "19:40 - 21:10", callbackData: "3_19:40")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "20:00 - 21:30", callbackData: "3_20:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "20:05 - 21:35", callbackData: "3_20:05")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "21:40 - 23:10", callbackData: "3_21:40")}
    });

    public static InlineKeyboardMarkup inlineBuildingKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "1", callbackData: "4_1"),
            InlineKeyboardButton.WithCallbackData(text: "2", callbackData: "4_2"),
            InlineKeyboardButton.WithCallbackData(text: "3", callbackData: "4_3"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "4", callbackData: "4_4"),
            InlineKeyboardButton.WithCallbackData(text: "5", callbackData: "4_5"),
            InlineKeyboardButton.WithCallbackData(text: "6", callbackData: "4_6"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "7", callbackData: "4_7"),
            InlineKeyboardButton.WithCallbackData(text: "8", callbackData: "4_8"),
            InlineKeyboardButton.WithCallbackData(text: "Все", callbackData: "4_all"),
        }
    });

    public static InlineKeyboardMarkup inlineAutoBuildingKeyboard = new(
        new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "1", callbackData: "4_1_auto"),
                InlineKeyboardButton.WithCallbackData(text: "2", callbackData: "4_2_auto"),
                InlineKeyboardButton.WithCallbackData(text: "3", callbackData: "4_3_auto"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "4", callbackData: "4_4_auto"),
                InlineKeyboardButton.WithCallbackData(text: "5", callbackData: "4_5_auto"),
                InlineKeyboardButton.WithCallbackData(text: "6", callbackData: "4_6_auto"),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "7", callbackData: "4_7_auto"),
                InlineKeyboardButton.WithCallbackData(text: "8", callbackData: "4_8_auto"),
                InlineKeyboardButton.WithCallbackData(text: "Все", callbackData: "4_all_auto"),
            }
        });
    
    public static InlineKeyboardMarkup inlineAllAudiences = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Показать все свободные аудитории", callbackData: "5_all"),
        }
    });

    public static InlineKeyboardMarkup inlineYNRoomKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Да", callbackData: "6_y"),
            InlineKeyboardButton.WithCallbackData(text: "Нет", callbackData: "6_n")
        }
    });

    public static InlineKeyboardMarkup inlineRestartKeyboard = new(new[]
    {
        InlineKeyboardButton.WithCallbackData("Повторить", "7_y"),
        InlineKeyboardButton.WithCallbackData("В начало", "7_n")
    });
    
    public static InlineKeyboardMarkup Back = new(new[]
    {
        InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "b")
    });

    public static ReplyKeyboardMarkup firstChoice = new(new[]
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
    })
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = true
    };
}