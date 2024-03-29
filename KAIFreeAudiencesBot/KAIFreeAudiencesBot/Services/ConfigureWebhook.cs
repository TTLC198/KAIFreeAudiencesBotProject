﻿using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using KAIFreeAudiencesBot.Models;
using Telegram.Bot.Types;

namespace KAIFreeAudiencesBot.Services;

public class ConfigureWebhook  : IHostedService
{
    private readonly ILogger<ConfigureWebhook> _logger;
    private readonly IServiceProvider _services;
    private readonly BotConfiguration _botConfig;

    public ConfigureWebhook(ILogger<ConfigureWebhook> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _services = serviceProvider;
        _botConfig = configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        var hostAddress = String.IsNullOrEmpty(_botConfig.HostAddress)
            ? await _services.GetRequiredService<NgrokService>().GetNgrokPublicUrl()
            : _botConfig.HostAddress;
        
        var webhookAddress = @$"{hostAddress}/bot/{_botConfig.BotApiKey}";
        _logger.LogInformation("Setting webhook: {webhookAddress}", webhookAddress);
        await botClient.SetWebhookAsync(
            url: webhookAddress,
            //certificate: new InputFile(new FileStream("//cert//cert.pem", FileMode.Open), "cert.pem"),
            allowedUpdates: Array.Empty<UpdateType>(),
            cancellationToken: cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();

        // Remove webhook upon app shutdown
        _logger.LogInformation("Removing webhook");
        await botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
    }
}