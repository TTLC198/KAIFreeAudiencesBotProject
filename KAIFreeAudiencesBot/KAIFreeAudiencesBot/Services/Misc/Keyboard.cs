using Telegram.Bot.Types.ReplyMarkups;

namespace KAIFreeAudiencesBot.Services.Misc;

public static class Keyboard
{
    public static InlineKeyboardMarkup inlineModeKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Автоматический ввод", callbackData: "0_auto"),
            InlineKeyboardButton.WithCallbackData(text: "Ручной ввод", callbackData: "0_manual"),
        }
    });

    public static InlineKeyboardMarkup inlineWeekKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Чет", callbackData: "1_e"),
            InlineKeyboardButton.WithCallbackData(text: "Нечет", callbackData: "1_n"),
        },
        new [] {
            InlineKeyboardButton.WithCallbackData(text: "Текущая", callbackData: "1_now")
        }
    });

    public static InlineKeyboardMarkup inlineDayKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Пн", callbackData: "3_mon"),
            InlineKeyboardButton.WithCallbackData(text: "Вт", callbackData: "3_tue"),
            InlineKeyboardButton.WithCallbackData(text: "Ср", callbackData: "3_wed")
            
        },
        new[] {
            InlineKeyboardButton.WithCallbackData(text: "Чт", callbackData: "3_thur"),
            InlineKeyboardButton.WithCallbackData(text: "Пт", callbackData: "3_fri"),
            InlineKeyboardButton.WithCallbackData(text: "Сб", callbackData: "3_sat")
        },
    });

    public static InlineKeyboardMarkup inlineTimeKeyboard = new(new[]
    {
        new[] {InlineKeyboardButton.WithCallbackData(text: "8:00 - 9:30", callbackData: "4_8:00 - 9:30")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "9:40 - 11:10", callbackData: "4_9:40 - 11:10")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "11:20 - 12:50", callbackData: "4_11:20 - 12:50")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "13:30 - 15:00", callbackData: "4_13:30 - 15:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "15:10 - 16:40", callbackData: "4_15:10 - 16:40")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "16:50 - 18:20", callbackData: "4_16:50 - 18:20")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:30 - 20:00", callbackData: "4_18:30 - 20:00")}
    });

    public static InlineKeyboardMarkup inlineYNKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Да", callbackData: "5_y"),
            InlineKeyboardButton.WithCallbackData(text: "Нет", callbackData: "5_n")
        }
    });

    public static InlineKeyboardMarkup inlineBuildingKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "1", callbackData: "6_1"),
            InlineKeyboardButton.WithCallbackData(text: "2", callbackData: "6_2"),
            InlineKeyboardButton.WithCallbackData(text: "3", callbackData: "6_3"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "4", callbackData: "6_4"),
            InlineKeyboardButton.WithCallbackData(text: "5", callbackData: "6_5"),
            InlineKeyboardButton.WithCallbackData(text: "6", callbackData: "6_6"),
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "7", callbackData: "6_7"),
            InlineKeyboardButton.WithCallbackData(text: "8", callbackData: "6_8"),
            InlineKeyboardButton.WithCallbackData(text: "Все", callbackData: "6_all"),
        }
    });

    public static InlineKeyboardMarkup inlineAutoBuildingKeyboard = new(
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
                InlineKeyboardButton.WithCallbackData(text: "Все", callbackData: "6_all_auto"),
            }
        });

    public static InlineKeyboardMarkup inlineYNRoomKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Да", callbackData: "7_y"),
            InlineKeyboardButton.WithCallbackData(text: "Нет", callbackData: "7_n")
        }
    });

    public static InlineKeyboardMarkup inlineAllRightKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Все хорошо", callbackData: "8_y"),
            InlineKeyboardButton.WithCallbackData(text: "Изменить", callbackData: "8_n")
        }
    });

    public static ReplyKeyboardMarkup firstChoice = new(new[]
    {
        new KeyboardButton[]
        {
            char.ConvertFromUtf32(0x1F193) + " Узнать свободные аудитории", //FREE
            char.ConvertFromUtf32(0x1F4C5) + " Расписание" //Calendar
        }
    })
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = true
    };

    public static ReplyKeyboardMarkup Back = new(new[]
    {
        new KeyboardButton[] {"Назад"}
    })
    {
        ResizeKeyboard = true
    };
}