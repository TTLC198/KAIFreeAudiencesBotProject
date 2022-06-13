namespace KAIFreeAudiencesBot.Models;

public class BotConfiguration
{
    //https://api.telegram.org/bot(mytoken)/setWebhook?url=https://mywebpagetorespondtobot/mymethod
    //https://api.telegram.org/5193287451:AAGEz71lTGBJe8BfJs8FdZ6_QDhXADOwgAI/setWebhook?url=https://11ad-91-245-38-201.ngrok.io

    /// <summary>
    /// Bot Api key from BotFather
    /// </summary>
    public string BotApiKey { get; init; }
}