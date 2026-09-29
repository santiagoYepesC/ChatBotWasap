using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatsAppBot.Admin.Services;
using WhatsAppBot.Shared.DTOs;

namespace WhatsAppBot.Admin.Pages.Settings.FrequentResponses;

[Authorize]
public sealed class IndexModel(IWhatsAppBotApiClient apiClient) : PageModel
{
    public IReadOnlyList<FrequentResponseDTO> Responses { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await apiClient.GetFrequentResponseAsync(id, cancellationToken);
            if (response?.Success != true || response.Data is null)
            {
                ErrorMessage = response?.Error?.Message ?? "No se pudo consultar la respuesta frecuente.";
                await LoadAsync(cancellationToken);
                return Page();
            }

            var updated = await apiClient.SetFrequentResponseActiveAsync(
                id, !response.Data.IsActive, cancellationToken);
            if (updated?.Success != true)
            {
                ErrorMessage = updated?.Error?.Message ?? "No se pudo cambiar el estado.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se pudo contactar la API. El estado no se modificó.";
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.DeleteFrequentResponseAsync(id, cancellationToken);
        }
        catch (AdminApiException exception)
        {
            ErrorMessage = exception.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se pudo contactar la API. La respuesta no se eliminó.";
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await apiClient.ListFrequentResponsesAsync(1, 100, cancellationToken);
            if (response?.Success == true && response.Data is not null)
            {
                Responses = response.Data.Items;
            }
            else
            {
                ErrorMessage ??= response?.Error?.Message ?? "No se pudo cargar la lista de respuestas.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage ??= "No se pudo contactar la API.";
        }
    }
}
