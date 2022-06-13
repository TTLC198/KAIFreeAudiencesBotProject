using KAIFreeAudiencesBot.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace KAIFreeAudiencesBot.Controllers;

public class WebhookController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromServices] HandleUpdateService handleUpdateService,
        [FromBody] Update update)
    {
        await handleUpdateService.EchoAsync(update);
        return Ok();
    }
}