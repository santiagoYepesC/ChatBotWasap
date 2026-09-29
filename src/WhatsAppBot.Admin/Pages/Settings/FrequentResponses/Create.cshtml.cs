using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatsAppBot.Admin.Services;
using WhatsAppBot.Shared.DTOs;

namespace WhatsAppBot.Admin.Pages.Settings.FrequentResponses;

[Authorize]
public sealed class CreateModel(IWhatsAppBotApiClient apiClient) : PageModel
{
    [BindProperty]
    public FrequentResponseForm Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var response = await apiClient.CreateFrequentResponseAsync(Input.ToRequest(), cancellationToken);
            if (response?.Success != true)
            {
                ErrorMessage = response?.Error?.Message ?? "No se pudo crear la respuesta frecuente.";
                return Page();
            }

            return RedirectToPage("./Index");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se pudo contactar la API. La respuesta no se creó.";
            return Page();
        }
    }
}

public sealed class FrequentResponseForm
{
    [Required, StringLength(500)]
    public string QuestionOrIntent { get; set; } = string.Empty;

    [Required]
    public string Expressions { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string AnswerText { get; set; } = string.Empty;

    [Range(0, 1000)]
    public int Priority { get; set; }

    [StringLength(100)]
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public FrequentResponseRequestDTO ToRequest() => new()
    {
        QuestionOrIntent = QuestionOrIntent,
        Expressions = Expressions.Split(
            ['\r', '\n'],
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
        AnswerText = AnswerText,
        Priority = Priority,
        Category = Category,
        IsActive = IsActive
    };
}
