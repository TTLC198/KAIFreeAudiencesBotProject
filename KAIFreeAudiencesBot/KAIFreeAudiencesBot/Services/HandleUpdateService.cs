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
            _ => UnknownMessageHandlerAsync(update.Message!)
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
                "/start" or "Назад" => OnStart(message),
                "/sh" => Development(message, message.Text!.Split(' ')),
                _ => UnknownCommandMessageAsync(message)
            };

            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {sentMessageId}", sentMessage.MessageId);
        }
        catch (Exception exception)
        {
            await HandleErrorAsync(exception);
        }
    }
    private async Task<Message> UnknownCommandMessageAsync(Message message)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Я не знаю такую команду"
        );
    }
    
    private async Task<Message> Development(Message message, string[] args)
    {
        string text = String.Empty;

        ScheduleParser parser = new ScheduleParser(new Logger<ScheduleParser>(new LoggerFactory()));

        await parser.ParseScheduleAsync();
        
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: text,
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> OnStart(Message message)
    {
        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            replyMarkup: Keyboard.firstChoice,
            text:
            "Привет пользователь! Я бот помошник, помогу найти тебе свободную аудиторию! Выбери дальнейшее действие!",
            cancellationToken: CancellationToken.None
        );
    }

    private async Task<Message> UnknownMessageHandlerAsync(Message message)
    {
        return await _botClient.SendTextMessageAsync(chatId: message.Chat.Id,
            text: char.ConvertFromUtf32(0x26A0) + "Произошла ошибка на стороне сервера");
    }
}