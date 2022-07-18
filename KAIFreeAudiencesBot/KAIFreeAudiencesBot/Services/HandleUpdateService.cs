using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using KAIFreeAudiencesBot.Models;
using KAIFreeAudiencesBot.Services.Database;
using KAIFreeAudiencesBot.Services.Misc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


namespace KAIFreeAudiencesBot.Services;

public class HandleUpdateService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<HandleUpdateService> _logger;
    private readonly IServiceProvider _services;

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
        
        try
        {
            Task<Message>? action = null;
            action = callbackQuery.Data![0] switch
            {
                '0' => SetMode(callbackQuery.Message!, callbackQuery.Data.Split('_')),
                //'1' =>,
                //'2' =>,
                //'3' =>,
                //'4' =>,
                //'5' => await _botClient,
                '6' => GetFreeAudiences(callbackQuery.Message!, callbackQuery.Data.Split('_')),
                //'7' =>,
                _ => ErrorMessageAsync(callbackQuery.Message!)
            };

            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {sentMessageId}", sentMessage.MessageId);
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }

        async Task<Message> SetMode(Message message, string[] args)
        {
            if (args[1] == "auto")
            {
                return await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    replyMarkup: Keyboard.inlineAutoBuildingKeyboard,
                    text:
                    "Выберите здание",
                    cancellationToken: CancellationToken.None
                );
            }
            else
            {
                return await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    replyMarkup: Keyboard.inlineWeekKeyboard,
                    text:
                    "Выберите чётность недели",
                    cancellationToken: CancellationToken.None
                );
            }
        }
    }

    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation("Receive message type: {messageType}", message.Type);

        try
        {
            var temp = char.ConvertFromUtf32(0x1F193);
            Task<Message>? action = null;
            action = message.Text!.Split(' ')[0] switch
            {
                "/start" or "Назад" => OnStart(message),
                "/sh" => Development(message, message.Text!.Split(' ')),
                "/select" => GetFreeAudiences(message, message.Text!.Split(' ')),
                var s when s[0] == char.ConvertFromUtf32(0x1F193)[0] => ChooseMode(message), //FREE Auditories
                _ => UnknownMessageAsync(message)
            };

            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {sentMessageId}", sentMessage.MessageId);
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }

        async Task<Message> OnStart(Message message)
        {
            return await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                replyMarkup: Keyboard.firstChoice,
                text:
                "Привет пользователь! Я бот помошник, помогу найти тебе свободную аудиторию! Выбери дальнейшее действие!",
                cancellationToken: CancellationToken.None
            );
        }

        async Task<Message> ChooseMode(Message message)
        {
            return await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                replyMarkup: Keyboard.inlineModeKeyboard,
                text:
                "Окей, поняли. Теперь необходимо определить режим работы!\n" +
                "Автоматический ввод - я выдам все свободные аудитории в данный момент, автоматически определив время и дату,\n" +
                "Ручной ввод - я задам у тебя пару вопросов, чтобы точно определить что тебе необходимо!",
                cancellationToken: CancellationToken.None
            );
        }

        async Task<Message> Development(Message message, string[] args)
        {
            string text = String.Empty;

            using (var db = _services.GetService<SchDbContext>())
            {
                //DateTime current = DateTime.Now;
                DateTime current = new DateTime(2022, 05, 30, 9, 0, 0);
                var schedules = db.scheduleSubjectDates
                    .AsNoTracking()
                    .Select(s => new {s.TimeInterval, s.date, s.Classroom})
                    .AsEnumerable()
                    .Where(s =>
                        s.TimeInterval.start < current.TimeOfDay
                        && current.TimeOfDay < s.TimeInterval.end
                        && s.date == DateOnly.FromDateTime(current.Date))
                    .ToList();

                var emptyClassrooms = db.classrooms
                    .AsNoTracking()
                    .Select(cr => new {cr.name, cr.building})
                    .AsEnumerable()
                    .Where(cr =>
                        !schedules.Any(s => s.Classroom.name == cr.name && s.Classroom.building == cr.building))
                    .ToList();
            }

            return await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: text,
                cancellationToken: CancellationToken.None
            );
        }
    }

    public async Task<Message> GetFreeAudiences(Message message, string[] args)
    {
        List<string> FreeAudItems = new List<string>()
        {
            "Аудитория;Здание"
        };

        var loadingTaskCts = new CancellationTokenSource();
        var loadingTask = new Task(async () =>
        {
            while (!loadingTaskCts.Token.IsCancellationRequested)
            {
                for (int i = 1; i < 4; i++)
                {
                    if (loadingTaskCts.Token.IsCancellationRequested) return;
                    await _botClient.EditMessageTextAsync(
                        chatId: message.Chat.Id,
                        messageId: message.MessageId,
                        text: "Получение данных" + "...".Substring(0, i),
                        cancellationToken: CancellationToken.None
                    );
                    Thread.Sleep(300);
                }
            }
        }, loadingTaskCts.Token);

        var dbTask = new Task(async () =>
        {
            await using var db = _services.GetService<SchDbContext>();
            
            DateTime current = DateTime.Now;

            if (args != null! && args[2] == "auto")
            {
                #if DEBUG
                current = new DateTime(2022, 05, 30, 9, 0, 0);
                #else
                current = DateTime.Now;
                #endif
            }
            
            var schedules = db.scheduleSubjectDates
                .AsNoTracking()
                .Select(s => new {s.TimeInterval, s.date, s.Classroom, s.Group})
                .AsEnumerable()
                .Where(s =>
                    s.TimeInterval.start < current.TimeOfDay
                    && current.TimeOfDay < s.TimeInterval.end
                    && s.date == DateOnly.FromDateTime(current.Date))
                .ToList();

            var emptyClassrooms = db.classrooms
                .AsNoTracking()
                .AsEnumerable()
                .Where(cr =>
                    !schedules.Any(s => s.Classroom.name == cr.name && s.Classroom.building == cr.building))
                .Select(cr => new { cr.building, cr.name })
                .AsEnumerable()
                .OrderBy(cr => cr.building)
                .ThenBy(cr => cr.name)
                .ToList();
            
            if (args != null! && args[1] != "" && args[1] != "all") emptyClassrooms = emptyClassrooms.Where(cr => cr.building == args[1]).ToList();

            foreach (var classroom in emptyClassrooms)
            {
                FreeAudItems.Add($"{classroom.name};{classroom.building}");
            }
        });

        try
        {
            loadingTask.Start();
            dbTask.Start();
            await Task.WhenAny(dbTask).ContinueWith(_ => { loadingTaskCts.Cancel(); });
        }
        catch (Exception e)
        {
            await HandleErrorAsync(e);
        }
        finally
        {
            loadingTask.Dispose();
            dbTask.Dispose();
        }

        //Длина строки создаваемой таблицы = 99 символов
        //tg позволяет отправлять сообщения длиной не более 4096 символов, следовательно количество строк в одном сообщении не должно превышать 40
        const int maxRowCount = 40;
        if (FreeAudItems.Count > maxRowCount)
        {
            int i = maxRowCount;
            do
            {
                var text = BuildTelegramTable(
                    FreeAudItems
                        .Take(new Range(
                            i,
                            i += FreeAudItems.Count - i < maxRowCount ? FreeAudItems.Count - i : maxRowCount))
                        .Prepend(FreeAudItems[0]).ToList(), autoColumnWidth: true,
                    minimumColumnWidth: 1, columnPadLeft: 1, columnPadRight: 1);
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: text,
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            } while (i < FreeAudItems.Count);

            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Таблица всех свободных аудиторий\n" + BuildTelegramTable(FreeAudItems.Take(maxRowCount).ToList(),
                    autoColumnWidth: true, minimumColumnWidth: 1, columnPadLeft: 1, columnPadRight: 1),
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
        else
        {
            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Таблица всех свободных аудиторий\n" + BuildTelegramTable(FreeAudItems, autoColumnWidth: true,
                    minimumColumnWidth: 1, columnPadLeft: 1, columnPadRight: 1),
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
    }

    private async Task<Message> UnknownMessageAsync(Message message)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: char.ConvertFromUtf32(0x2754) + " Я не знаю такую команду"
        );
    }

    private async Task<Message> ErrorMessageAsync(Message message)
    {
        return await _botClient.SendTextMessageAsync(chatId: message.Chat.Id,
            text: char.ConvertFromUtf32(0x26A0) + " Произошла ошибка на стороне сервера");
    }

    public string BuildTelegramTable(
        List<string> table_lines,
        string tableColumnSeparator = "|", char inputArraySeparator = ';',
        int maxColumnWidth = 0, bool fixedColumnWidth = false, bool autoColumnWidth = false,
        int minimumColumnWidth = 4, int columnPadRight = 0, int columnPadLeft = 0,
        bool beginEndBorders = true)
    {
        var prereadyTable = new List<string>() {"<pre>"};
        var columnsWidth = new List<int>();
        var firstLine = table_lines[0];
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
                    table_lines.ForEach(line =>
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
        foreach (var line in table_lines)
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