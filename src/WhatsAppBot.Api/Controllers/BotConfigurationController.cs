using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Entities;
using WhatsAppBot.Shared.DTOs;
using WhatsAppBot.Shared.Enums;
using WhatsAppBot.Shared.Responses;

namespace WhatsAppBot.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/whatsapp/bot-configuration")]
public sealed class BotConfigurationController(IBotConfigurationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseE<BotConfigurationDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseE<BotConfigurationDTO>>> Get(CancellationToken cancellationToken)
    {
        var configuration = await service.GetAsync(cancellationToken);
        return Ok(ResponseE<BotConfigurationDTO>.Ok(Map(configuration), HttpContext.TraceIdentifier));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ResponseE<BotConfigurationDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseE<object?>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResponseE<BotConfigurationDTO>>> Update(
        [FromBody] UpdateBotConfigurationRequestDTO request, CancellationToken cancellationToken)
    {
        var configuration = await service.UpdateAsync(
            request.IsBotEnabled!.Value, request.ReplyMode!.Value, cancellationToken);
        return Ok(ResponseE<BotConfigurationDTO>.Ok(Map(configuration), HttpContext.TraceIdentifier));
    }

    private static BotConfigurationDTO Map(BotConfiguration configuration) => new()
    {
        IsBotEnabled = configuration.IsBotEnabled,
        ReplyMode = configuration.ReplyMode
    };
}
