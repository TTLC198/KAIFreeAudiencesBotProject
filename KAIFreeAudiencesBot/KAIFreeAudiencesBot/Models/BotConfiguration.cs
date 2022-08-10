namespace KAIFreeAudiencesBot.Models;

public class BotConfiguration
{
    //https://api.telegram.org/bot(mytoken)/setWebhook?url=https://mywebpagetorespondtobot/mymethod

    /// <summary>
    /// Bot Api key from BotFather
    /// </summary>
    public string BotApiKey { get; init; }
    /// <summary>
    /// HostAddress from ngrok
    /// </summary>
    public string HostAddress { get; init; }
}