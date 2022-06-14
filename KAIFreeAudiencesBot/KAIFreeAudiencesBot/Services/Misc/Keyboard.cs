using Telegram.Bot.Types.ReplyMarkups;

namespace KAIFreeAudiencesBot.Services.Misc;

public static class Keyboard
{
    private static InlineKeyboardMarkup inlineModeKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Автоматический ввод", callbackData: "0_Автоматический ввод"),
            InlineKeyboardButton.WithCallbackData(text: "Ручной ввод", callbackData: "0_Ручной ввод"),
        }
    });

    public static InlineKeyboardMarkup inlineWeekKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Чет", callbackData: "1_Четная"),
            InlineKeyboardButton.WithCallbackData(text: "Нечет", callbackData: "1_Нечетная"),
        }
    });

    public static InlineKeyboardMarkup inlineDayKeyboard = new(new[]
    {
        new[] {InlineKeyboardButton.WithCallbackData(text: "Пн", callbackData: "2_Понедельник")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "Вт", callbackData: "2_Вторник")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "Ср", callbackData: "2_Среда")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "Чт", callbackData: "2_Четверг")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "Пт", callbackData: "2_Пятница")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "Сб", callbackData: "2_Суббота")},
    });

    public static InlineKeyboardMarkup inlineTimeKeyboard = new(new[]
    {
        new[] {InlineKeyboardButton.WithCallbackData(text: "8:00 - 9:30", callbackData: "3_8:00 - 9:30")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "9:40 - 11:10", callbackData: "3_9:40 - 11:10")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "11:20 - 12:50", callbackData: "3_11:20 - 12:50")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "13:30 - 15:00", callbackData: "3_13:30 - 15:00")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "15:10 - 16:40", callbackData: "3_15:10 - 16:40")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "16:50 - 18:20", callbackData: "3_16:50 - 18:20")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "18:30 - 20:00", callbackData: "3_18:30 - 20:00")}
    });

    public static InlineKeyboardMarkup inlineYNBuildingKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Да", callbackData: "4_Yes"),
            InlineKeyboardButton.WithCallbackData(text: "Нет", callbackData: "4_No")
        }
    });

    public static InlineKeyboardMarkup inlineBuildingKeyboard = new(new[]
    {
        new[] {InlineKeyboardButton.WithCallbackData(text: "1", callbackData: "5_1")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "2", callbackData: "5_2")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "3", callbackData: "5_3")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "4", callbackData: "5_4")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "5", callbackData: "5_5")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "6", callbackData: "5_6")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "7", callbackData: "5_7")},
        new[] {InlineKeyboardButton.WithCallbackData(text: "8", callbackData: "5_8")},
    });

    public static InlineKeyboardMarkup inlineYNRoomKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Да", callbackData: "6_Yes"),
            InlineKeyboardButton.WithCallbackData(text: "Нет", callbackData: "6_No")
        }
    });

    public static InlineKeyboardMarkup inlineAllRightKeyboard = new(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(text: "Все хорошо", callbackData: "7_Yes"),
            InlineKeyboardButton.WithCallbackData(text: "Изменить", callbackData: "7_No")
        }
    });

    public static ReplyKeyboardMarkup firstChoice = new(new[]
    {
        new KeyboardButton[] {"Узнать свободные аудитории", "Расписание"}
    })
    {
        ResizeKeyboard = true
    };
    public static ReplyKeyboardMarkup Back = new(new[]
    {
        new KeyboardButton[] {"Назад"}
    })
    {
        ResizeKeyboard = true
    };
}