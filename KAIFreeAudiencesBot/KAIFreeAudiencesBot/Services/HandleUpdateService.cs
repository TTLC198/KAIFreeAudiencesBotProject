using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KAIFreeAudiencesBot.Models;
using KAIFreeAudiencesBot.Services.Database;
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
        Client currentClient = new Client() { id = clientId };
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
            Task<Message> action = callbackQuery.Data![0] switch
            {
                'b' => OnRestart(callbackQuery.Message!, currentClient),
                '0' => Call(() => callbackQuery.Data!.Split('_')[1] == "days"
                    ? ChooseParity(callbackQuery.Message!, currentClient)
                    : ErrorMessageAsync(callbackQuery.Message!)),
                '1' => SwitchKey(callbackQuery.Message!, currentClient, callbackQuery.Data),
                '2' => ChooseDay(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')),
                '3' => ChooseTime(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')),
                '4' => ChooseBuilding(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')),
                '5' => Call(() => //Выбор аудитории
                {
                    var args = callbackQuery.Data.Split('_');
                    if (args[1] == "all")
                    {
                        currentClient.step = ClientSteps.Default;
                        return NotRealized(callbackQuery.Message!);
                        //return GetFreeAudiences(callbackQuery.Message!, currentClient.settings);
                    }
                    else
                    {
                        return ChooseAudience(callbackQuery.Message!, currentClient, args);
                    }
                }),
                '6' => Call(() => //Все аудитории в виде таблицы
                {
                    if (currentClient.step == ClientSteps.ChooseAudience)
                    {
                        currentClient.step = ClientSteps.Default;
                        return NotRealized(callbackQuery.Message!);
                        //return GetFreeAudiences(callbackQuery.Message!, currentClient.settings);
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
                '8' => callbackQuery.Data.Split('_')[1] switch
                {
                    "continue" => CheckAudience(callbackQuery.Message!, currentClient.settings),
                    "change" => ChooseAudience(callbackQuery.Message!, currentClient, callbackQuery.Data.Split('_')),
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
            action = currentClient.step switch
            {
                ClientSteps.ChooseAudience =>
                    ParseAudience(message, currentClient),

                ClientSteps.ChooseTime =>
                    ChooseTime(message, currentClient, null!),

                _ =>
                    message.Text!.Split(' ')[0] switch
                    {
                        "/start" =>
                            OnStart(message, currentClient),
                        var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F193) || s == "/free" =>
                            ChooseMode(message), //Свободные аудитории
                        var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F4D1) || s == "/parity" =>
                            GetWeekParity(message), //Четность недели
                        var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F3EB) || s == "/audiences" =>
                            GetAllAudiences(message), //Все аудитории
                        var s when s.Split(' ')[0] == char.ConvertFromUtf32(0x1F4C5) || s == "/schedule" =>
                            NotRealized(message), //Расписание
                        _ =>
                            UnknownMessageAsync(message)
                    }
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

    private async Task<Message> SwitchKey(Message message, Client client, string args)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        var keyboard = message.ReplyMarkup;
        var indexes = Misc.GetIndexes(keyboard!, args);

        Misc.ChangeValue(
            clients[clientIndex].step,
            clients[clientIndex].settings,
            keyboard!.InlineKeyboard.ToList()[indexes[0]].ToList()[indexes[1]]);

        keyboard = Misc.UpdateKeyboardMarkup(
            clients[clientIndex].step,
            clients[clientIndex].settings,
            keyboard);

        return await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            replyMarkup: keyboard,
            text: message.Text!,
            cancellationToken: CancellationToken.None
        );
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
            text:
            "Окей, поняли. Теперь необходимо определить режим работы!\n" +
            "Автоматический ввод - я выдам все свободные аудитории в данный момент, автоматически определив время и дату,\n" +
            "Ручной ввод - я задам у тебя пару вопросов, чтобы точно определить что тебе необходимо!",
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> ChooseParity(Message message, Client client)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseParity;
        clients[clientIndex].settings.Mode = Modes.SpecificDaysOfWeek;

        await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: "Выбран режим " + clients[clientIndex].settings.Mode
        );

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
        if (clients[clientIndex].settings.Parity.Count == 0)
        {
            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Вы не выбрали четность недели" + '\n' +
                      "Выберите четность недели",
                replyMarkup: Keyboard.inlineWeekKeyboard
            );
        }

        clients[clientIndex].step = ClientSteps.ChooseDay;
        clients[clientIndex].settings.Parity = clients[clientIndex].settings.Parity
            .OrderBy(parity => int.Parse(Enum.Format(typeof(Parity), parity, "d"))).ToList();

        await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: "Выбранные четности: " + string.Join(' ', clients[clientIndex].settings.Parity)
        );

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
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
                    .Where(k => TimeOnly.ParseExact(k.First().CallbackData!.Split('_')[1], new[] { "HH:mm", "H:mm" })
                        .Hour == inputHour);
                var enumerable = choices as IEnumerable<InlineKeyboardButton>[] ?? choices.ToArray();
                if (!enumerable.Any())
                    return await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Введенного временого промежутка не существует!",
                        replyMarkup: Keyboard.inlineRestartKeyboard
                    );
                else
                    return await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Выберите один из предложенных ниже вариантов",
                        replyMarkup: new InlineKeyboardMarkup(enumerable)
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
            if (clients[clientIndex].settings.DaysOfWeek.Count == 0)
            {
                return await _botClient.EditMessageTextAsync(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    text: "Вы не выбрали дни недели" + '\n' +
                          "Выберите дни недели",
                    replyMarkup: Keyboard.inlineDayKeyboard
                );
            }

            clients[clientIndex].step = ClientSteps.ChooseTime;
            clients[clientIndex].settings.DaysOfWeek = clients[clientIndex].settings.DaysOfWeek
                .OrderBy(day => int.Parse(Enum.Format(typeof(DayOfWeek), day, "d"))).ToList();

            await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Выбранные дни недели: " + string.Join(' ', clients[clientIndex].settings.DaysOfWeek)
            );

            return await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Введите час начала занятия"
            );
        }
    }

    private async Task<Message> ChooseBuilding(Message message, Client client, string[] args)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseBuilding;
        clients[clientIndex].settings.TimeStart =
            TimeOnly.ParseExact(args[1], new[] { "HH:mm", "H:mm" }, new CultureInfo("ru-RU"));

        await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: "Вы выбрали временной промежуток: " + clients[clientIndex].settings.TimeStart + "-" +
                  clients[clientIndex].settings.TimeStart.AddMinutes(90).ToString()
        );

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            replyMarkup: Keyboard.inlineAutoBuildingKeyboard,
            text: "Выберите здание",
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> ChooseAudience(Message message, Client client, string[] args)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        if (clients[clientIndex].step == ClientSteps.ChooseBuilding)
        {
            clients[clientIndex].settings.Building = args[1] != "all"
                ? Enum.GetValues(typeof(Buildings)).Cast<Buildings>().ToList()[int.Parse(args[1])]
                : Buildings.All;
            await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Вы выбрали здания: " + clients[clientIndex].settings.Building
            );
        }

        clients[clientIndex].step = ClientSteps.ChooseAudience;
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Отправьте номера аудитории через запятую для проверки их занятости",
            replyMarkup: Keyboard.inlineAllAudiences
        );
    }

    private async Task<Message> ParseAudience(Message message, Client client)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        var audiences = message.Text!;
        if (Regex.IsMatch(audiences, "[A-z]", RegexOptions.IgnoreCase))
        {
            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Введена неправильная аудитория",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: "8_change"),
                })
            );
        }

        var newAudience
            = audiences.Split(',').Select(aud => aud.Trim()).Distinct().ToList();
        await using (var scope = _services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetService<SchDbContext>();
            var building = Enum.Format(typeof(Buildings), clients[clientIndex].settings.Building, "d");
            var existingAudience =
                db!.classrooms
                    .Where(classroom => newAudience.Contains(classroom.name) && classroom.building == building)
                    .Select(classroom => classroom.name).ToList();
            var notExistingAud = newAudience.Except(existingAudience).ToList();
            if (notExistingAud.Count != 0)
            {
                clients[clientIndex].settings.Audience = newAudience.Intersect(existingAudience).ToList();
                return await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Аудитории {string.Join(", ", notExistingAud)} не существуют",
                    replyMarkup: Keyboard.InlineChangeAudKeyboard
                );
            }
            else clients[clientIndex].step = ClientSteps.Default;
        }

        clients[clientIndex].settings.Audience = newAudience;
        return await CheckAudience(message, clients[clientIndex].settings);
    }

    private async Task<Message> CheckAudience(Message message, ClientSettings settings)
    {
        var freeAudItems = new List<(string audience, string building, string date, string timeInterval)>();

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
            await using (var scope = _services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetService<SchDbContext>();

                var tempDates = Misc.GetDates(
                    settings!.DaysOfWeek,
                    settings!.DateStart ?? db!.defaultValues
                        .AsNoTracking()
                        .ToList()[0]
                        .value,
                    settings!.DateEnd ?? db!.defaultValues
                        .AsNoTracking()
                        .ToList()[1]
                        .value,
                    settings.Parity
                );

                var schedules = db!.scheduleSubjectDates
                    .AsNoTracking()
                    .Include(s => s.TimeInterval)
                    .Include(s => s.Classroom)
                    .AsEnumerable()
                    .Where(s =>
                        s.TimeInterval.start == settings.TimeStart
                        && tempDates.Contains(s.date))
                    .ToList();

                if (schedules == null!)
                {
                    currentMessage = await _botClient.EditMessageTextAsync(
                        chatId: message.Chat.Id,
                        messageId: message.MessageId,
                        text:
                        "В данный момент невозможно определить наличие свободных аудиторий, так как отсутвует расписание занятий.",
                        cancellationToken: CancellationToken.None
                    );
                }

                if (settings!.Building != Buildings.All)
                {
                    schedules = schedules!.Where(schedule =>
                            schedule.Classroom.building == Convert.ToString((int)settings.Building)
                            && settings!.Audience.Contains(schedule.Classroom.name))
                        .AsEnumerable()
                        .OrderBy(schedule => schedule.Classroom.building)
                        .ThenBy(schedule => schedule.Classroom.name)
                        .ThenBy(schedule => schedule.date)
                        .ToList();
                }
                else
                {
                    schedules = schedules!
                        .OrderBy(schedule => schedule.Classroom.building)
                        .ThenBy(schedule => schedule.Classroom.name)
                        .ThenBy(schedule => schedule.date)
                        .ToList();
                }

                freeAudItems.AddRange(Misc.SmashDates(schedules));

                switch (freeAudItems.Count)
                {
                    case 1:
                        currentMessage = await _botClient.EditMessageTextAsync(
                            chatId: message.Chat.Id,
                            messageId: loadingMessage.MessageId,
                            replyMarkup: Keyboard.Back,
                            text: char.ConvertFromUtf32(0x274c) + "Введенные аудитории заняты"
                        );
                        break;
                    case 2:
                        currentMessage = await _botClient.EditMessageTextAsync(
                            chatId: message.Chat.Id,
                            messageId: loadingMessage.MessageId,
                            replyMarkup: Keyboard.Back,
                            text: char.ConvertFromUtf32(0x2714) + "Введенная аудитория свободна"
                        );
                        break;
                    default:
                        StringBuilder tableStringBuilder = new StringBuilder();
                        using (var table = new Table(tableStringBuilder))
                        {
                            using var headerRow = table.AddHeaderRow();
                            headerRow.AddCell("Аудитория");
                            headerRow.AddCell("Здание");
                            headerRow.AddCell("Даты");
                            headerRow.AddCell("Время");
                            foreach (var classroom in freeAudItems)
                            {
                                using var row = table.AddRow();
                                row.AddCell(classroom.audience);
                                row.AddCell(classroom.building);
                                row.AddCell(classroom.date);
                                row.AddCell(classroom.timeInterval);
                            }
                        }

                        currentMessage = await _botClient.EditMessageTextAsync(
                            chatId: message.Chat.Id,
                            messageId: loadingMessage.MessageId,
                            text: "Таблица свободных аудиторий:"
                        );
                        await _botClient.SendPhotoAsync(
                            chatId: message.Chat.Id,
                            photo: Misc.HtmlToImageStreamConverter(
                                @"<style>
                table {
                    font-family: 'Lucida Sans Unicode', 'Lucida Grande', Sans-Serif;
                    border-collapse: collapse;
                    color: #686461;
                    margin: 0 auto;
                }
                caption {
                    padding: 10px;
                    color: white;
                    background: #8FD4C1;
                    font-size: 18px;
                    text-align: left;
                    font-weight: bold;
                }
                th {
                    border-bottom: 3px solid #B9B29F;
                    padding: 10px;
                    text-align: center;
                }
                td {
                    padding: 10px;
                }
                tr:nth-child(odd) {
                    background: white;
                }
                tr:nth-child(even) {
                    background: #E8E6D1;
                }</style>" +
                                tableStringBuilder
                            )!);
                        break;
                }
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
        Stream tableImageStream = new MemoryStream();

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
            StringBuilder tableStringBuilder = new StringBuilder();
            await using var db = _services.GetService<SchDbContext>();

            var classrooms = db!.classrooms
                .AsNoTracking()
                .AsEnumerable()
                .OrderBy(cr => cr.building)
                .ThenBy(cr => cr.name)
                .ToList();

            using (var table = new Table(tableStringBuilder))
            {
                using var headerRow = table.AddHeaderRow();
                headerRow.AddCell("Аудитория");
                headerRow.AddCell("Здание");
                foreach (var classroom in classrooms)
                {
                    using var row = table.AddRow();
                    row.AddCell(classroom.name);
                    row.AddCell(classroom.building);
                }
            }

            tableImageStream = Misc.HtmlToImageStreamConverter(
                "<style>table, th, td { border: 1px solid black; margin: 0 auto; font-size: 36px; }</style>" +
                tableStringBuilder,
                new Size()
            )!;
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

        await _botClient.EditMessageTextAsync(
            chatId: message.Chat.Id,
            messageId: loadingMessage.MessageId,
            text: "Таблица аудиторий: "
        );

        return await _botClient.SendPhotoAsync(
            chatId: message.Chat.Id,
            photo: tableImageStream!,
            parseMode: ParseMode.Html,
            replyMarkup: Keyboard.Back,
            cancellationToken: CancellationToken.None
        );
    }

    /*private async Task<Message> GetFreeAudiences(Message message, ClientSettings settings)
    {
        List<string> freeAudItems = new List<string>()
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

            
            var schedules = db!.scheduleSubjectDates
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
            
            if (schedules == null! || emptyClassrooms == null!)
            {
                freeAudItems = null!;
                return;
            }
            
            if (settings != null!) 
                if (settings.Building != Buildings.All) 
                    emptyClassrooms = emptyClassrooms.Where(cr => cr.building == Convert.ToString((int)settings.Building)).ToList();

            foreach (var classroom in emptyClassrooms)
            {
                freeAudItems.Add($"{classroom.name};{classroom.building}");
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

        if (freeAudItems == null!)
        {
            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "В данный момент невозможно определить наличие свободных аудиторий, так как отсутвует расписание занятий.",
                cancellationToken: CancellationToken.None
            );
        }

        //Длина строки создаваемой таблицы = 99 символов
        //tg позволяет отправлять сообщения длиной не более 4096 символов, следовательно количество строк в одном сообщении не должно превышать 40
        const int maxRowCount = 40;
        if (freeAudItems.Count > maxRowCount)
        {
            int i = maxRowCount;
            do
            {
                var text = BuildTelegramTable(
                    freeAudItems
                        .Take(new Range(
                            i,
                            i += freeAudItems.Count - i < maxRowCount ? freeAudItems.Count - i : maxRowCount))
                        .Prepend(freeAudItems[0]).ToList(), 
                    fixedColumnWidth: true, maxColumnWidth: 14,
                    minimumColumnWidth: 14, columnPadLeft: 1, columnPadRight: 1);
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: text,
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            } while (i < freeAudItems.Count);
            return await _botClient.EditMessageTextAsync(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "Таблица свободных аудиторий" + BuildTelegramTable(freeAudItems.Take(maxRowCount).ToList(),
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
                text: "Таблица свободных аудиторий" + BuildTelegramTable(freeAudItems, 
                    fixedColumnWidth: true, maxColumnWidth: 14,
                    minimumColumnWidth: 14, columnPadLeft: 1, columnPadRight: 1),
                parseMode: ParseMode.Html,
                replyMarkup: Keyboard.Back,
                cancellationToken: CancellationToken.None
            );
        }
    }*/

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
}