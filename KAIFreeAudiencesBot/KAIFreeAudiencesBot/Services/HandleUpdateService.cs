using System.Globalization;
using KAIFreeAudiencesBot.Models;
using KAIFreeAudiencesBot.Services.Database;
using KAIFreeAudiencesBot.Services.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


namespace KAIFreeAudiencesBot.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private readonly IServiceProvider _services;

    private static List<Client> clients = new List<Client>();
    
    TRes Call<TRes>(Func<TRes> f) => f(); // Для удобства

    public HandleUpdateService(ITelegramBotClient botClient, ILogger<HandleUpdateService> logger,
        IServiceProvider services)
    {
        _botClient = botClient;
        _logger = logger;
        _services = services;
    }


    private Task HandleErrorAsync(Exception exception)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }

    public async Task EchoAsync(Update update)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => BotOnMessageReceived(update.Message!),
            UpdateType.CallbackQuery => BotOnCallbackQueryReceived(update.CallbackQuery!),
            _ => UnknownMessageAsync(update.Message!)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }

    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        _logger.LogInformation("Receive callback query with data: {0}", callbackQuery.Data);

        long clientId = callbackQuery.From.Id;
        Client currentClient = new Client() { id = clientId};
        if (clients.Any(c => c.id == clientId))
        {
            currentClient = clients.Find(c => c.id == clientId)!;
        }
        else
        {
            clients.Add(new Client()
            {
                id = clientId
            });
        }
            
        try
        {
            Task<Message> action = callbackQuery.Data![0] switch
            {
                'b' => OnRestart(callbackQuery.Message!, currentClient),
                '0' => Call(() => callbackQuery.Data!.Split('_')[1] == "general"
                    ? ChooseParity(callbackQuery.Message!, currentClient)
                    : ErrorMessageAsync(callbackQuery.Message!)), 
                '1' => SwitchKey(callbackQuery.Message!, callbackQuery.Data),
                '2' => ChooseDay(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')),
                _ => ErrorMessageAsync(callbackQuery.Message!)
            };
            Message sentMessage = await action;
            clients[clients.FindIndex(c => c.id == clientId)] = currentClient;
            
            _logger.LogInformation("The message was sent with id: {sentMessageId}", sentMessage.MessageId);
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation("Receive message type: {messageType}", message.Type);

        long clientId = message.From!.Id;
        Client currentClient;
        if (clients.Count(c => c.id == clientId) > 0)
        {
            currentClient = clients.Find(c => c.id == clientId)!;
        }
        else
        {
            clients.Add(new Client()
            {
                id = clientId
            });
            currentClient = clients.Find(c => c.id == clientId)!;
        }
        
        try
        {
            Task<Message>? action;
            action = message.Text!.Split(' ')[0] switch
            {
                "/start" => OnStart(message, currentClient),
                var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F193) || s == "/free" => ChooseMode(message), //Свободные аудитории
                var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F4D1) || s == "/parity" => GetWeekParity(message), //Четность недели
                var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F3EB) || s == "/audiences" => NotRealized(message), //Все аудитории
                var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F4C5) || s == "/schedule" => NotRealized(message), //Расписание
                _ => UnknownMessageAsync(message)
            };
            Message sentMessage = await action;
            clients[clients.FindIndex(c => c.id == clientId)] = currentClient;
            
            _logger.LogInformation("The message was sent with id: {sentMessageId}", sentMessage.MessageId);
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }
    
    private async Task<Message> OnStart(Message message, Client client)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.Default;
        clients[clientIndex].settings = new ClientSettings();
        
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            replyMarkup: Keyboard.firstChoice,
            text:
            "Привет пользователь! Я бот помощник, помогу найти тебе свободную аудиторию! Выбери дальнейшее действие!",
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> OnRestart(Message message, Client client)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.Default;
        clients[clientIndex].settings = new ClientSettings();
        
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            replyMarkup: Keyboard.firstChoice,
            text: "Выбери дальнейшее действие!",
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> NotRealized(Message message)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text:
            "На данный момент эта функция не реализована.",
            cancellationToken: CancellationToken.None
        );
    }
    
    private async Task<Message> GetWeekParity(Message message)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Данная неделя " +
                  (Misc.GetWeekParity(DateTime.Now) == Parity.NotEven ? "<i>нечетная</i>" : "<i>четная</i>") + ".",
            replyMarkup: Keyboard.Back,
            parseMode: ParseMode.Html,
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> ChooseMode(Message message)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            replyMarkup: Keyboard.inlineModeKeyboard,
            text: "Выбери режим",
            cancellationToken: CancellationToken.None
        );
    }
    
    private async Task<Message> ChooseParity(Message message, Client client)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseParity;
        clients[clientIndex].settings.Mode = Modes.General; //заглушка ПОМЕНЯТЬ

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            replyMarkup: Keyboard.inlineWeekKeyboard,
            text: "Выберите чётность недели",
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> ChooseDay(Message message, Client client, string[] args)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseDay;
        var parities = new List<Parity>();
        var buttons = message.ReplyMarkup!.InlineKeyboard.ToList()[0].ToList();
        if (buttons[0].Text.Split(' ')[0] == "✅")
        {
            parities.Add(Parity.Even);
        }
        if (buttons[1].Text.Split(' ')[0] == "✅")
        {
            parities.Add(Parity.NotEven);
        }

        clients[clientIndex].settings.Parity = parities;
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Выберите день недели",
            replyMarkup: Keyboard.inlineDayKeyboard
        );
    }
    
    private static int[] GetIndexes(InlineKeyboardMarkup keyboardMarkup, string match)
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
    
    private async Task<Message> SwitchKey(Message message, string args)
    {
        var keyboard = message.ReplyMarkup;
        var indexes = GetIndexes(keyboard, args);
        var text = keyboard!.InlineKeyboard.ToList()[indexes[0]].ToList()[indexes[1]].Text;
        text = text.Split(' ')[0] switch
        {
            "☑" => text.Replace("☑", "✅"),
            "✅" => text.Replace("✅", "☑"),
            _ => text
        };
        keyboard!.InlineKeyboard.ToList()[indexes[0]].ToList()[indexes[1]].Text = text;
        return await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            replyMarkup: keyboard,
            text: "Выберите чётность недели",
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> UnknownMessageAsync(Message message)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: char.ConvertFromUtf32(0x2753) + " Я не знаю такую команду"
        );
    }

    private async Task<Message> ErrorMessageAsync(Message message)
    {
        return await _botClient.SendTextMessageAsync(chatId: message.Chat.Id,
            text: char.ConvertFromUtf32(0x26A0) + " Произошла ошибка на стороне сервера");
    }

    public string BuildTelegramTable(
        List<string> tableLines,
        string tableColumnSeparator = "|", char inputArraySeparator = ';',
        int maxColumnWidth = 0, bool fixedColumnWidth = false, bool autoColumnWidth = false,
        int minimumColumnWidth = 4, int columnPadRight = 0, int columnPadLeft = 0,
        bool beginEndBorders = true)
    {
        var prereadyTable = new List<string>() {"<pre>"};
        var columnsWidth = new List<int>();
        var firstLine = tableLines[0];
        var lineVector = firstLine.Split(inputArraySeparator);

        if (fixedColumnWidth && maxColumnWidth == 0)
            throw new ArgumentException("For fixedColumnWidth usage must set maxColumnWidth > 0");
        else if (fixedColumnWidth && maxColumnWidth > 0)
        {
            for (var x = 0; x < lineVector.Length; x++)
                columnsWidth.Add(maxColumnWidth + columnPadRight + columnPadLeft);
        }
        else
        {
            for (var x = 0; x < lineVector.Length; x++)
            {
                var columnData = lineVector[x].Trim();
                var columnFullLength = columnData.Length;

                if (autoColumnWidth)
                    tableLines.ForEach(line =>
                        columnFullLength = line.Split(inputArraySeparator)[x].Length > columnFullLength
                            ? line.Split(inputArraySeparator)[x].Length
                            : columnFullLength);

                columnFullLength = columnFullLength < minimumColumnWidth ? minimumColumnWidth : columnFullLength;

                var columnWidth = columnFullLength + columnPadRight + columnPadLeft;

                if (maxColumnWidth > 0 && columnWidth > maxColumnWidth)
                    columnWidth = maxColumnWidth;

                columnsWidth.Add(columnWidth);
            }
        }
        foreach (var line in tableLines)
        {
            lineVector = line.Split(inputArraySeparator);

            var fullLine = new string[lineVector.Length + (beginEndBorders ? 2 : 0)];
            if (beginEndBorders) fullLine[0] = "";

            for (var x = 0; x < lineVector.Length; x++)
            {
                var clearedData = lineVector[x].Trim();
                var dataLength = clearedData.Length;
                var columnWidth = columnsWidth[x];
                var columnSizeWithoutTrimSize = columnWidth - columnPadRight - columnPadLeft;
                var dataCharsToRead = columnSizeWithoutTrimSize > dataLength ? dataLength : columnSizeWithoutTrimSize;
                var columnData = clearedData.Substring(0, dataCharsToRead);
                columnData = columnData.PadRight(columnData.Length + columnPadRight);
                columnData = columnData.PadLeft(columnData.Length + columnPadLeft);

                var column = columnData.PadRight(columnWidth);

                fullLine[x + (beginEndBorders ? 1 : 0)] = column;
            }

            if (beginEndBorders) fullLine[fullLine.Length - 1] = "";

            prereadyTable.Add(string.Join(tableColumnSeparator, fullLine));
        }

        prereadyTable.Add("</pre>");

        return string.Join("\r\n", prereadyTable);
    }
}