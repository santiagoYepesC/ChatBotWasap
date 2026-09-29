using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatsAppBot.Admin.Services;

namespace WhatsAppBot.Admin.Pages.Settings.FrequentResponses;

[Authorize]
public sealed class EditModel(IWhatsAppBotApiClient apiClient) : PageModel
{
    [BindProperty]
    public FrequentResponseForm Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public long Id { get; set; }

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (Id < 1)
        {
            return NotFound();
        }

        try
        {
            var response = await apiClient.GetFrequentResponseAsync(Id, cancellationToken);
            if (response?.Success != true || response.Data is null)
            {
                ErrorMessage = response?.Error?.Message ?? "No se pudo cargar la respuesta frecuente.";
                return Page();
            }

            Input = new FrequentResponseForm
            {
                QuestionOrIntent = response.Data.QuestionOrIntent,
                Expressions = string.Join(Environment.NewLine, response.Data.Expressions),
                AnswerText = response.Data.AnswerText,
                Priority = response.Data.Priority,
                Category = response.Data.Category,
                IsActive = response.Data.IsActive
            };
            return Page();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se pudo contactar la API.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Id < 1)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var response = await apiClient.UpdateFrequentResponseAsync(Id, Input.ToRequest(), cancellationToken);
            if (response?.Success != true)
            {
                ErrorMessage = response?.Error?.Message ?? "No se pudo guardar la respuesta frecuente.";
                return Page();
            }

            return RedirectToPage("./Index");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se pudo contactar la API. La respuesta no se guardó.";
            return Page();
        }
    }
}
