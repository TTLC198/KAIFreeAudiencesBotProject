using KAIFreeAudiencesBot.Services.Misc;
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

    private static string[] _resultStrings = new string[8];

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
            //UpdateType.CallbackQuery => QueryUpdate(_botClient, update.CallbackQuery!),
            _ => UnknownMessageHandlerAsync(_botClient, update.Message!)
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

    private async Task BotOnMessageReceived(Message message)
    {
        _logger.LogInformation("Receive message type: {messageType}", message.Type);

        try
        {
            Task<Message>? action = null;
            action = message.Text!.Split(' ')[0] switch
            {
                "/start" or "Назад" => OnStart(_botClient, message),
                "/sh" => Development(_botClient, message, message.Text!.Split(' ')),
                _ => UnknownCommandMessageAsync(_botClient, message)
            };

            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {sentMessageId}", sentMessage.MessageId);
        }
        catch (Exception e)
        {
            _logger.LogError($"Something went wrong:\n{e.Message}");
        }
    }
    private async Task<Message> UnknownCommandMessageAsync(ITelegramBotClient botClient, Message message)
    {
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Я не знаю такую команду"
        );
    }
    
    private async Task<Message> Development(ITelegramBotClient botClient, Message message, string[] args)
    {
        string text = String.Empty;

        if (args[1] != String.Empty)
        {
            ScheduleParser parser = new ScheduleParser(new Logger<ScheduleParser>(new LoggerFactory()));

            var groupIds = await parser.GetGroupsIdAsync(args[1]);
            foreach (var groupId in groupIds)
            {
                //if ()
            }
        }
        
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: text,
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> OnStart(ITelegramBotClient botClient, Message message)
    {
        _resultStrings = new string[8];
        return await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            replyMarkup: Keyboard.firstChoice,
            text:
            "Привет пользователь! Я бот помошник, помогу найти тебе свободную аудиторию! Выбери дальнейшее действие!",
            cancellationToken: CancellationToken.None
        );
    }

    private static async Task<Message> UnknownMessageHandlerAsync(ITelegramBotClient botClient, Message message)
    {
        return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
            text: char.ConvertFromUtf32(0x26A0) + "Произошла ошибка на стороне сервера");
    }
}