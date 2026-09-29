using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatsAppBot.Admin.Services;
using WhatsAppBot.Shared.DTOs;
using WhatsAppBot.Shared.Enums;

namespace WhatsAppBot.Admin.Pages.Settings.WhatsApp;

[Authorize]
public sealed class IndexModel(IWhatsAppBotApiClient apiClient) : PageModel
{
    public WhatsAppIntegrationDTO? Integration { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    [BindProperty, Required, MaxLength(2048)]
    public string AuthorizationCode { get; set; } = string.Empty;

    [BindProperty, Required, MaxLength(64)]
    public string WabaId { get; set; } = string.Empty;

    [BindProperty, Required, MaxLength(64)]
    public string PhoneNumberId { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadIntegrationAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostCompleteSignupAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadIntegrationAsync(cancellationToken);
            ErrorMessage = "Meta no devolvió los datos necesarios para completar la conexión.";
            return Page();
        }

        try
        {
            var response = await apiClient.CompleteWhatsAppSignupAsync(
                new EmbeddedSignupCompletionRequestDTO
                {
                    AuthorizationCode = AuthorizationCode,
                    WabaId = WabaId,
                    PhoneNumberId = PhoneNumberId
                },
                cancellationToken);
            if (response?.Success != true || response.Data is null)
            {
                ErrorMessage = response?.Error?.Message ?? "No se pudo completar la conexión de Meta.";
                await LoadIntegrationAsync(cancellationToken);
                return Page();
            }

            Integration = response.Data;
            SuccessMessage = "WhatsApp Business quedó conectado mediante el proceso oficial de Meta.";
            return Page();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se pudo contactar la API. La conexión no fue confirmada.";
            await LoadIntegrationAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.DisconnectWhatsAppAsync(cancellationToken);
            SuccessMessage = "La integración se desconectó y el bot quedó desactivado.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se pudo contactar la API. La integración no fue modificada.";
        }

        await LoadIntegrationAsync(cancellationToken);
        return Page();
    }

    public string ConnectionLabel(ConnectionState? state) => state switch
    {
        ConnectionState.NotConnected or null => "No conectado",
        ConnectionState.Configuring => "Pendiente",
        ConnectionState.Connected => "Conectado",
        ConnectionState.Error => "Error",
        _ => "Estado desconocido"
    };

    private async Task LoadIntegrationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await apiClient.GetWhatsAppIntegrationAsync(cancellationToken);
            if (response?.Success == true)
            {
                Integration = response.Data;
            }
            else
            {
                ErrorMessage ??= response?.Error?.Message ?? "No se pudo consultar el estado de WhatsApp.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage ??= "No se pudo contactar la API.";
        }
    }
}
