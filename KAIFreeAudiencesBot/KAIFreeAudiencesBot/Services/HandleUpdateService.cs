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
        Client currentClient = new Client() {id = clientId};
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
                        currentClient.settings.Mode = Modes.SpecificDaysAllAudiences;
                        return CheckAudience(callbackQuery.Message!, currentClient.settings);
                    }
                    else
                    {
                        return ChooseAudience(callbackQuery.Message!, currentClient, args);
                    }
                }),
                '6' => Call(() => //Все аудитории в виде таблицы
                {
                    if (currentClient.step == ClientSteps.ChooseBuildingAllAudiences)
                    {
                        if (int.TryParse(callbackQuery.Data.Split('_')[1], out var buildingNumber))
                        {
                            foreach (Buildings building in Enum.GetValues(typeof(Buildings)))
                                if (buildingNumber == (int) building)
                                    currentClient.settings.Building = building;
                            return GetAllAudiences(callbackQuery.Message!, currentClient.settings);
                        }
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
                            ChooseBuildingAllAudiences(message, currentClient), //Все аудитории
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
            text: message.Text!
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
                    .Where(k => TimeOnly.ParseExact(k.First().CallbackData!.Split('_')[1], new[] {"HH:mm", "H:mm"})
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
            TimeOnly.ParseExact(args[1], new[] {"HH:mm", "H:mm"}, new CultureInfo("ru-RU"));

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

    private async Task<Message> ChooseBuildingAllAudiences(Message message, Client client)
    {
        var clientIndex = clients.FindIndex(cl => cl.id == client.id);
        clients[clientIndex].step = ClientSteps.ChooseBuildingAllAudiences;

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            replyMarkup: Keyboard.inlineBuildingKeyboard,
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
            return await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
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
        var audienceTuples = new List<(string audience, string building, string date, string timeInterval)>();

        var loadingMessage = _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Получение данных",
            cancellationToken: CancellationToken.None
        ).Result;

        var loadingTaskCts = new CancellationTokenSource();
        var loadingTask = CreateLoadingTask(
            loadingMessage,
            loadingTaskCts,
            message.Chat.Id,
            loadingMessage.Text!);

        var currentMessage = new Message();

        var dbTask = new Task(async () =>
        {
            await using var scope = _services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
            var db = scope.ServiceProvider.GetService<SchDbContext>();

            var tempDates = Misc.GetDates(
                settings!.DaysOfWeek,
                settings!.DateStart ?? Misc.GetDefaultValues(settings.Parity, false, null),
                settings!.DateEnd ?? Misc.GetDefaultValues(null, true, null),
                settings.Parity
            );

            if (settings.Mode == Modes.SpecificDaysAllAudiences)
                settings.Audience = db!.classrooms
                    .AsNoTracking()
                    .AsEnumerable()
                    .Where(c =>
                        c.building == Convert.ToString((int) settings.Building))
                    .Select(c =>
                        c.name)
                    .ToList();

            var schedules = db!.scheduleSubjectDates
                .AsNoTracking()
                .Include(s => s.TimeInterval)
                .Include(s => s.Classroom)
                .AsEnumerable()
                .Where(s =>
                    s.TimeInterval.start == settings.TimeStart &&
                    s.Classroom.building == Convert.ToString((int) settings.Building) &&
                    tempDates.Contains(s.date))
                .ToList();

            if (!db.scheduleSubjectDates.Any())
            {
                currentMessage = await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text:
                    "В данный момент невозможно определить наличие свободных аудиторий, так как отсутвует расписание занятий."
                );
                await _botClient.DeleteMessageAsync(
                    chatId: loadingMessage.Chat.Id,
                    messageId: loadingMessage.MessageId
                );
                return;
            }

            foreach (var classroom in settings.Audience)
            {
                if (!schedules.Exists(s => s.Classroom.name == classroom))
                {
                    audienceTuples.Add(new ValueTuple<string, string, string, string>
                    (
                        item1: classroom,
                        item2: Convert.ToString((int) settings.Building),
                        item3: settings.DateStart.ToString()!,
                        item4: settings.TimeStart.ToString()!
                    ));
                }
            }

            switch (audienceTuples.Count)
            {
                case 0:
                    currentMessage = await _botClient.EditMessageTextAsync(
                        chatId: message.Chat.Id,
                        messageId: loadingMessage.MessageId,
                        replyMarkup: Keyboard.Back,
                        text: char.ConvertFromUtf32(0x274c) + "Введенные аудитории заняты"
                    );
                    break;
                case 1:
                    currentMessage = await _botClient.EditMessageTextAsync(
                        chatId: message.Chat.Id,
                        messageId: loadingMessage.MessageId,
                        replyMarkup: Keyboard.Back,
                        text: char.ConvertFromUtf32(0x2714) + "Введенная аудитория свободна"
                    );
                    break;
                default:
                    Task imgGenTask = null;
                    try
                    {
                        imgGenTask = new Task(async () =>
                        {
                            int length = audienceTuples.Count;
                            for (int i = 1; i <= length / 13 + 1; i++)
                            {
                                StringBuilder tableStringBuilder = new StringBuilder();
                                using (var table = new Table(tableStringBuilder, style: Table.DefaultStyleTable))
                                {
                                    using var headerRow = table.AddHeaderRow();
                                    headerRow.AddCell("Аудитория");
                                    headerRow.AddCell("Здание");
                                    headerRow.AddCell("Даты");
                                    headerRow.AddCell("Время");
                                    var start = (i - 1) * 13;
                                    foreach (var classroom in audienceTuples.GetRange(start,
                                                 Math.Min(13, length - start)))
                                    {
                                        using var row = table.AddRow();
                                        row.AddCell(classroom.audience);
                                        row.AddCell(classroom.building);
                                        row.AddCell(classroom.date);
                                        row.AddCell(classroom.timeInterval);
                                    }

                                    await _botClient.SendPhotoAsync(
                                        chatId: message.Chat.Id,
                                        caption: i < 2 ? "Таблица свободных аудиторий:" : String.Empty,
                                        photo: Misc.HtmlToImageStreamConverter(tableStringBuilder.ToString(),
                                            new Size(460,
                                                (audienceTuples.GetRange(start, Math.Min(13, length - start))
                                                     .Count +
                                                 1) * 45))!);
                                }
                            }
                        });

                        imgGenTask.Start();

                        await Task.WhenAny(imgGenTask).ContinueWith(async _ =>
                        {
                            await _botClient.DeleteMessageAsync(
                                chatId: loadingMessage.Chat.Id,
                                messageId: loadingMessage.MessageId
                            );
                        });
                    }
                    catch (Exception e)
                    {
                        HandleErrorAsync(e);
                    }
                    finally
                    {
                        imgGenTask.Dispose();
                    }

                    break;
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

    private async Task<Message> GetAllAudiences(Message message, ClientSettings settings)
    {
        var audienceTuples = new List<(string audience, string building)>();

        var loadingMessage = _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Получение данных",
            cancellationToken: CancellationToken.None
        ).Result;

        var loadingTaskCts = new CancellationTokenSource();
        var loadingTask = CreateLoadingTask(
            loadingMessage,
            loadingTaskCts,
            message.Chat.Id,
            loadingMessage.Text!);

        var currentMessage = new Message();

        var dbTask = new Task(async () =>
        {
            await using (var scope = _services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetService<SchDbContext>();
                var building = Convert.ToString((int) settings.Building);
                var allClassrooms = db!.classrooms
                    .Where(c => building == "0" || c.building == building)
                    .ToList();

                foreach (var classroom in allClassrooms)
                {
                    audienceTuples.Add(new ValueTuple<string, string>
                    (
                        item1: classroom.name,
                        item2: classroom.building
                    ));
                }

                switch (audienceTuples.Count)
                {
                    case 0:
                        currentMessage = await _botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text:
                            "В данный момент невозможно определить наличие аудиторий, так как отсутвует расписание занятий."
                        );
                        await _botClient.DeleteMessageAsync(
                            chatId: loadingMessage.Chat.Id,
                            messageId: loadingMessage.MessageId
                        );
                        break;
                    default:
                        Task imgGenTask = null;
                        try
                        {
                            imgGenTask = new Task(async () =>
                            {
                                int length = audienceTuples.Count;
                                for (int i = 1; i <= length / 13 + 1; i++)
                                {
                                    StringBuilder tableStringBuilder = new StringBuilder();
                                    using (var table = new Table(tableStringBuilder, style: Table.DefaultStyleTable))
                                    {
                                        using var headerRow = table.AddHeaderRow();
                                        headerRow.AddCell("Аудитория");
                                        headerRow.AddCell("Здание");
                                        var start = (i - 1) * 13;
                                        foreach (var classroom in audienceTuples.GetRange(start,
                                                     Math.Min(13, length - start)))
                                        {
                                            using var row = table.AddRow();
                                            row.AddCell(classroom.audience);
                                            row.AddCell(classroom.building);
                                        }

                                        await _botClient.SendPhotoAsync(
                                            chatId: message.Chat.Id,
                                            caption: i < 2 ? "Таблица аудиторий:" : String.Empty,
                                            photo: Misc.HtmlToImageStreamConverter(tableStringBuilder.ToString(),
                                                new Size(460,
                                                    (audienceTuples.GetRange(start, Math.Min(13, length - start))
                                                         .Count +
                                                     1) * 45))!);
                                    }
                                }
                            });

                            imgGenTask.Start();

                            await Task.WhenAny(imgGenTask).ContinueWith(async _ =>
                            {
                                await _botClient.DeleteMessageAsync(
                                    chatId: loadingMessage.Chat.Id,
                                    messageId: loadingMessage.MessageId
                                );
                            });
                        }
                        catch (Exception e)
                        {
                            HandleErrorAsync(e);
                        }
                        finally
                        {
                            imgGenTask.Dispose();
                        }

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

    private Task CreateLoadingTask(
        Message loadingMessage,
        CancellationTokenSource loadingTaskCtSource,
        long chatId,
        string text = "Получение данных",
        int delay = 300)
    {
        return new Task(async () =>
        {
            while (true)
            {
                for (int i = 1; i < 4; i++)
                {
                    if (loadingTaskCtSource.Token.IsCancellationRequested) return;
                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: loadingMessage.MessageId,
                        text: text + "...".Substring(0, i),
                        cancellationToken: CancellationToken.None
                    );
                    Thread.Sleep(delay);
                }
            }
        }, loadingTaskCtSource.Token);
    }
}