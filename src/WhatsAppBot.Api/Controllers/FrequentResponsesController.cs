using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Contracts;
using WhatsAppBot.Shared.DTOs;
using WhatsAppBot.Shared.Responses;

namespace WhatsAppBot.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/frequent-responses")]
public sealed class FrequentResponsesController(IFrequentResponseService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ResponseE<PagedResultDTO<FrequentResponseDTO>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseE<PagedResultDTO<FrequentResponseDTO>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await service.ListAsync(page, pageSize, cancellationToken);
        var response = new PagedResultDTO<FrequentResponseDTO>
        {
            Items = result.Items.Select(Map).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
        return Ok(ResponseE<PagedResultDTO<FrequentResponseDTO>>.Ok(response, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResponseE<FrequentResponseDTO>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ResponseE<FrequentResponseDTO>>> Create(
        [FromBody] FrequentResponseRequestDTO request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(ToCommand(request), cancellationToken);
        var response = ResponseE<FrequentResponseDTO>.Ok(Map(created), HttpContext.TraceIdentifier);
        return CreatedAtAction(nameof(Get), new { id = created.Response.FrequentResponseId }, response);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ResponseE<FrequentResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseE<object?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResponseE<FrequentResponseDTO>>> Get(
        long id, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(id, cancellationToken);
        return Ok(ResponseE<FrequentResponseDTO>.Ok(Map(result), HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ResponseE<FrequentResponseDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseE<FrequentResponseDTO>>> Update(
        long id, [FromBody] FrequentResponseRequestDTO request, CancellationToken cancellationToken)
    {
        var updated = await service.UpdateAsync(id, ToCommand(request), cancellationToken);
        return Ok(ResponseE<FrequentResponseDTO>.Ok(Map(updated), HttpContext.TraceIdentifier));
    }

    [HttpPatch("{id:long}/active")]
    [ProducesResponseType(typeof(ResponseE<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResponseE<bool>>> SetActive(
        long id, [FromBody] FrequentResponseStateDTO request, CancellationToken cancellationToken)
    {
        await service.SetActiveAsync(id, request.IsActive!.Value, cancellationToken);
        return Ok(ResponseE<bool>.Ok(true, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ResponseE<object?>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static FrequentResponseCommand ToCommand(FrequentResponseRequestDTO request) => new(
        request.QuestionOrIntent,
        request.Expressions!,
        request.AnswerText,
        request.Priority,
        request.Category,
        request.IsActive!.Value);

    private static FrequentResponseDTO Map(FrequentResponseRecord record) => new()
    {
        FrequentResponseId = record.Response.FrequentResponseId,
        QuestionOrIntent = record.Response.QuestionOrIntent,
        Expressions = record.Expressions,
        AnswerText = record.Response.AnswerText,
        Priority = record.Response.Priority,
        Category = record.Response.Category,
        IsActive = record.Response.IsActive,
        CreatedAtUtc = record.Response.CreatedAtUtc,
        ModifiedAtUtc = record.Response.ModifiedAtUtc
    };
}
