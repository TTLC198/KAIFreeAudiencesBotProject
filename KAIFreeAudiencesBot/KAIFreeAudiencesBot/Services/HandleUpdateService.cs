using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using KAIFreeAudiencesBot.Models;
using KAIFreeAudiencesBot.Services.Database;
using KAIFreeAudiencesBot.Services.Models;
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
        }
            
        try
        {
            Task<Message>? action = null;
            action = callbackQuery.Data![0] switch
            {
                'b' => OnRestart(callbackQuery.Message!, currentClient),
                '0' => Call(() => callbackQuery.Data!.Split('_')[1] == "auto"
                    ? ChooseBuilding(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')) // Выбор здания (начало автоматического режима)
                    : ChooseParity(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_'))), //Выбор четности недели (начало ручного режима)
                '1' => ChooseDay(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')), // Выбор дня недели
                '2' => ChooseTime(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')), // Выбор времени
                '3' => ChooseBuilding(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')), // Выбор здания
                '4' => Call(() => //Выбор аудитории
                {
                    var args = callbackQuery.Data.Split('_');
                    if (args[1] == "all")
                    {
                        currentClient.step = ClientSteps.Default;
                        return GetFreeAudiences(callbackQuery.Message!, currentClient.settings);
                    }
                    else
                    {
                        currentClient.step = ClientSteps.ChooseAudience;
                        return ChooseAudience(callbackQuery.Message!, currentClient, args);
                    }
                    
                }),
                '5' => Call(() => //Все аудитории в виде таблицы
                {
                    if (currentClient.step == ClientSteps.ChooseAudience)
                    {
                        currentClient.step = ClientSteps.Default;
                        return GetFreeAudiences(callbackQuery.Message!, currentClient.settings);
                    }
                    return _botClient.EditMessageTextAsync(
                        chatId: callbackQuery.From.Id,
                        messageId: callbackQuery.Message!.MessageId,
                        replyMarkup: null!,
                        text:
                        "Так не работает!",
                        cancellationToken: CancellationToken.None
                    );
                }),
                '7' => callbackQuery.Data.Split('_')[1] switch // Повтор при неудачном вводе времени
                {
                    "y" => ChooseTime(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')),
                    "n" => OnStart(callbackQuery.Message!, currentClient),
                    _ => ErrorMessageAsync(callbackQuery.Message!)
                },
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
        Client currentClient = new Client();
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
            Task<Message>? action = null;
            if (currentClient.step == ClientSteps.ChooseAudience)
            {
                currentClient.step = ClientSteps.Default;
                currentClient.settings.Audience = message.Text!;
                action = CheckAudience(message, currentClient.settings);
            }
            else if (currentClient.step == ClientSteps.ChooseTime)
            {
                action = ChooseTime(message, currentClient, null!);
            }
            else
            {
                var t1 = 
                action = message.Text!.Split(' ')[0] switch
                {
                    "/start" => OnStart(message, currentClient),
                    var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F193) || s == "/free" => ChooseMode(message), //Свободные аудитории
                    var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F4D1) || s == "/parity" => GetWeekParity(message), //Четность недели
                    var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F3EB) || s == "/audiences" => GetAllAudiences(message), //Все аудитории
                    var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F4C5) || s == "/schedule" => NotRealized(message), //Расписание
                    _ => UnknownMessageAsync(message)
                };
            }
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
            text: "Данная неделя " + (Misc.GetWeekParity(DateTime.Now) == Parity.NotEven ? "<i>нечетная</i>" : "<i>четная</i>") + ".",
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
            text:
            "Окей, поняли. Теперь необходимо определить режим работы!\n" +
            "Автоматический ввод - я выдам все свободные аудитории в данный момент, автоматически определив время и дату,\n" +
            "Ручной ввод - я задам у тебя пару вопросов, чтобы точно определить что тебе необходимо!",
            cancellationToken: CancellationToken.None
        );
    }
    
    private async Task<Message> ChooseParity(Message message, Client client, string[] args)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseParity;
        clients[clientIndex].settings.Mode = Modes.Manual;

        return await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            replyMarkup: Keyboard.inlineWeekKeyboard,
            text: "Выберите чётность недели",
            cancellationToken: CancellationToken.None
        );
    }
    
    private async Task<Message> ChooseDay(Message message, Client client, string[] args)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseDay;
        clients[clientIndex].settings.Parity = args[1] switch
        {
            "e" => Parity.Even,
            "n" => Parity.NotEven,
            "now" => Misc.GetWeekParity(DateTime.Now)
        };

        return await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: "Выберите день недели",
            replyMarkup: Keyboard.inlineDayKeyboard
        );
    }
    
    private async Task<Message> ChooseTime(Message message, Client client, string[] args)
    {
        if (client.step == ClientSteps.ChooseTime)
        {
            var clientIndex = clients.FindIndex(cl => cl.id == client.id);
            if (int.TryParse(message.Text, out var inputHour))
            {
                clients[clientIndex].step = ClientSteps.ChooseCorrectTime;
                var choices = Keyboard.inlineTimeKeyboard.InlineKeyboard
                    .Where(k => TimeOnly.ParseExact(k.First().CallbackData!.Split('_')[1], new[] {"HH:mm", "H:mm"}).Hour == inputHour);
                if (!choices.Any())
                    return await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Введенного временого промежутка не существует!",
                        replyMarkup: Keyboard.inlineRestartKeyboard
                    );  
                else
                    return await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Выберите один из предложенных ниже вариантов",
                        replyMarkup: new InlineKeyboardMarkup(choices!)
                    );
            }
            else
            {
                clients[clientIndex].step = ClientSteps.ChooseDay;
                return await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Вы ввели некоректное значение!",
                    replyMarkup: Keyboard.inlineRestartKeyboard
                );
            }
        }
        else
        {
            var clientIndex = clients.FindIndex(cl => cl.id == client.id);
            clients[clientIndex].step = ClientSteps.ChooseTime;
            if (args[0] != "7") clients[clientIndex].settings.Day = Enum.GetValues(typeof(Days)).Cast<Days>().ToList()[int.Parse(args[1])];

            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Введите час начала занятия"
            );
        }
    }

    private async Task<Message> ChooseBuilding(Message message, Client client, string[] args)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseBuilding;
        if (args[1] == "auto")
        {
            clients[clientIndex].settings.Mode = Modes.Auto;
        }
        else
        {
            clients[clientIndex].settings.TimeStart = TimeOnly.ParseExact(args[1], new[] {"HH:mm", "H:mm"}, new CultureInfo("ru-RU"));
        }

        return await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            replyMarkup: Keyboard.inlineAutoBuildingKeyboard,
            text: "Выберите здание",
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> ChooseAudience(Message message, Client client, string[] args)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseAudience;
        if (args[1] != "all")
            clients[clientIndex].settings.Building = Enum.GetValues(typeof(Buildings)).Cast<Buildings>().ToList()[int.Parse(args[1])];
        else 
            clients[clientIndex].settings.Building = Buildings.All;
            
        return await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: "Отправьте номер аудитории для проверки её занятости",
            replyMarkup: Keyboard.inlineAllAudiences
        );
    }

    private async Task<Message> CheckAudience(Message message, ClientSettings settings)
    {
        var loadingMessage = await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Получение данных",
            cancellationToken: CancellationToken.None
        );
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
                        messageId: loadingMessage.MessageId,
                        text: "Получение данных" + "...".Substring(0, i),
                        cancellationToken: CancellationToken.None
                    );
                    Thread.Sleep(300);
                }
            }
        }, loadingTaskCts.Token);

        var currentMessage = new Message();
        
        var dbTask = new Task(async () =>
        {
            await using var db = _services.GetService<SchDbContext>();
            
            DateTime current = DateTime.Now;

            if (settings != null!)
            {
                if (settings.Mode == Modes.Auto)
                {
#if DEBUG
                    current = new DateTime(2022, 05, 30, 9, 0, 0);
#else
                    current = DateTime.Now;
#endif
                }
                else
                {
                    current = new DateTime(
                        DateTime.Now.Year,
                        DateTime.Now.Month,
                        Misc.GetCurrentDay(DateTime.Now, settings.Parity)!.Value.Day,
                        settings.TimeStart.Hour,
                        settings.TimeStart.Minute,
                        settings.TimeStart.Second);
                }
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
                    !schedules.Any(s => s.Classroom.name == cr.name && s.Classroom.building == cr.building)
                    && String.Equals(cr.name, settings.Audience, StringComparison.OrdinalIgnoreCase))
                .Select(cr => new { cr.building, cr.name })
                .AsEnumerable()
                .OrderBy(cr => cr.building)
                .ThenBy(cr => cr.name)
                .ToList();
            
            if (settings != null!) 
                if (settings.Building != Buildings.All) 
                    emptyClassrooms = emptyClassrooms.Where(cr => cr.building == Convert.ToString((int)settings.Building)).ToList();

            if (db.classrooms
                .AsNoTracking()
                .AsEnumerable()
                .Count(cr => cr.name.Contains(settings.Audience)) == 0)
            {
                currentMessage = await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: loadingMessage.MessageId,
                    replyMarkup: Keyboard.Back,
                    text: char.ConvertFromUtf32(0x2753) + "Введенная аудитория не существует."
                );
            }
            else if (emptyClassrooms.Count == 0)
            {
                currentMessage = await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: loadingMessage.MessageId,
                    replyMarkup: Keyboard.Back,
                    text: char.ConvertFromUtf32(0x274c) + "Введенная аудитория занята"
                );
            }
            else
            {
                currentMessage = await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: loadingMessage.MessageId,
                    replyMarkup: Keyboard.Back,
                    text: char.ConvertFromUtf32(0x2714) + "Введенная аудитория свободна"
                );
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

        return currentMessage;
    }
    
    private async Task<Message> GetAllAudiences(Message message)
    {
        List<string> FreeAudItems = new List<string>();

        var loadingMessage = await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Получение данных",
            cancellationToken: CancellationToken.None
        );
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
                        messageId: loadingMessage.MessageId,
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

            var classrooms = db.classrooms
                .AsNoTracking()
                .AsEnumerable()
                .OrderBy(cr => cr.building)
                .ThenBy(cr => cr.name)
                .ToList();

            foreach (var classroom in classrooms)
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
            int i = 0;
            do
            {
                var text =
                    BuildTelegramTable(
                    FreeAudItems
                        .Take(new Range(
                            i,
                            i += FreeAudItems.Count - i < maxRowCount ? FreeAudItems.Count - i : maxRowCount)).ToList(),
                    fixedColumnWidth: true, maxColumnWidth: 14,
                    minimumColumnWidth: 14, columnPadLeft: 1, columnPadRight: 1);
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: text,
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            } while (i < FreeAudItems.Count);
            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: loadingMessage.MessageId,
                text: "Таблица всех аудиторий" + BuildTelegramTable(FreeAudItems.Take(maxRowCount).ToList(),
                    fixedColumnWidth: true, maxColumnWidth: 14,
                    minimumColumnWidth: 14, columnPadLeft: 1, columnPadRight: 1),
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
        else
        {
            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: loadingMessage.MessageId,
                text: "Таблица всех аудиторий" + BuildTelegramTable(FreeAudItems, fixedColumnWidth: true, maxColumnWidth: 14,
                    minimumColumnWidth: 14, columnPadLeft: 1, columnPadRight: 1),
                parseMode: ParseMode.Html,
                replyMarkup: Keyboard.Back,
                cancellationToken: CancellationToken.None
            );
        }
    }

    private async Task<Message> GetFreeAudiences(Message message, ClientSettings settings)
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

            if (settings != null!)
            {
                if (settings.Mode == Modes.Auto)
                {
#if DEBUG
                    current = new DateTime(2022, 05, 30, 9, 0, 0);
#else
                    current = DateTime.Now;
#endif
                }
                else
                {
#if DEBUG
                    current = new DateTime(
                        DateTime.Now.Year,
                        5,
                        Misc.GetCurrentDay(DateTime.Now, settings.Parity)!.Value.Day,
                        settings.TimeStart.Hour,
                        settings.TimeStart.Minute,
                        settings.TimeStart.Second);
#else
                    current = new DateTime(
                        DateTime.Now.Year,
                        DateTime.Now.Month,
                        Misc.GetCurrentDay(DateTime.Now, settings.Parity)!.Value.Day,
                        settings.TimeStart.Hour,
                        settings.TimeStart.Minute,
                        settings.TimeStart.Second);
#endif
                }
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
            
            if (settings != null!) 
                if (settings.Building != Buildings.All) 
                    emptyClassrooms = emptyClassrooms.Where(cr => cr.building == Convert.ToString((int)settings.Building)).ToList();

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
                        .Prepend(FreeAudItems[0]).ToList(), 
                    fixedColumnWidth: true, maxColumnWidth: 14,
                    minimumColumnWidth: 14, columnPadLeft: 1, columnPadRight: 1);
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
                text: "Таблица свободных аудиторий" + BuildTelegramTable(FreeAudItems.Take(maxRowCount).ToList(),
                    fixedColumnWidth: true, maxColumnWidth: 14,
                    minimumColumnWidth: 14, columnPadLeft: 1, columnPadRight: 1),
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
        else
        {
            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Таблица свободных аудиторий" + BuildTelegramTable(FreeAudItems, 
                    fixedColumnWidth: true, maxColumnWidth: 14,
                    minimumColumnWidth: 14, columnPadLeft: 1, columnPadRight: 1),
                parseMode: ParseMode.Html,
                replyMarkup: Keyboard.Back,
                cancellationToken: CancellationToken.None
            );
        }
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