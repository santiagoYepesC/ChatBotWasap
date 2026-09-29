using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatsAppBot.Admin.Services;
using WhatsAppBot.Shared.DTOs;
using WhatsAppBot.Shared.Enums;

namespace WhatsAppBot.Admin.Pages.Settings.Bot;

[Authorize]
public sealed class IndexModel(IWhatsAppBotApiClient apiClient) : PageModel
{
    [BindProperty]
    public bool IsBotEnabled { get; set; }

    [BindProperty, Required]
    public string ReplyMode { get; set; } = nameof(BotReplyMode.FaqThenAi);

    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await apiClient.GetBotConfigurationAsync(cancellationToken);
            if (response?.Success != true || response.Data is null)
            {
                ErrorMessage = response?.Error?.Message ?? "No se pudo consultar la configuración del bot.";
                return Page();
            }

            Apply(response.Data);
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
        if (!Enum.TryParse<BotReplyMode>(ReplyMode, ignoreCase: false, out var replyMode) ||
            !Enum.IsDefined(replyMode))
        {
            ModelState.AddModelError(nameof(ReplyMode), "Selecciona un modo válido.");
            return Page();
        }

        try
        {
            var response = await apiClient.UpdateBotConfigurationAsync(
                new UpdateBotConfigurationRequestDTO
                {
                    IsBotEnabled = IsBotEnabled,
                    ReplyMode = replyMode
                },
                cancellationToken);
            if (response?.Success != true || response.Data is null)
            {
                ErrorMessage = response?.Error?.Message ?? "No se pudo guardar la configuración del bot.";
                return Page();
            }

            Apply(response.Data);
            SuccessMessage = "La configuración del bot se guardó correctamente.";
            return Page();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se pudo contactar la API. La configuración no se guardó.";
            return Page();
        }
    }

    private void Apply(BotConfigurationDTO configuration)
    {
        IsBotEnabled = configuration.IsBotEnabled;
        ReplyMode = configuration.ReplyMode.ToString();
    }
}
