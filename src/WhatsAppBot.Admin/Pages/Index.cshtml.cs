using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatsAppBot.Admin.Services;
using WhatsAppBot.Shared.DTOs;

namespace WhatsAppBot.Admin.Pages;

[Authorize]
public sealed class IndexModel(IWhatsAppBotApiClient apiClient) : PageModel
{
    public BotConfigurationDTO? BotConfiguration { get; private set; }
    public string? ConfigurationError { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await apiClient.GetBotConfigurationAsync(cancellationToken);
            if (response?.Success == true)
            {
                BotConfiguration = response.Data;
            }
            else
            {
                ConfigurationError = response?.Error?.Message ?? "No se pudo consultar la configuración.";
            }
        }
        catch (HttpRequestException)
        {
            ConfigurationError = "No se pudo contactar la API.";
        }
    }
}
