using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using WhatsAppBot.Admin.Authentication;
using WhatsAppBot.Shared.DTOs;
using WhatsAppBot.Shared.Requests;
using WhatsAppBot.Shared.Responses;

namespace WhatsAppBot.Admin.Services;

public interface IWhatsAppBotApiClient
{
    Task<ResponseE<AuthResponseDTO>?> LoginAsync(LoginRequestDTO request, CancellationToken cancellationToken);
    Task<ResponseE<BotConfigurationDTO>?> GetBotConfigurationAsync(CancellationToken cancellationToken);
    Task<ResponseE<BotConfigurationDTO>?> UpdateBotConfigurationAsync(
        UpdateBotConfigurationRequestDTO request, CancellationToken cancellationToken);
    Task<ResponseE<PagedResultDTO<FrequentResponseDTO>>?> ListFrequentResponsesAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    Task<ResponseE<FrequentResponseDTO>?> GetFrequentResponseAsync(
        long id, CancellationToken cancellationToken);
    Task<ResponseE<FrequentResponseDTO>?> CreateFrequentResponseAsync(
        FrequentResponseRequestDTO request, CancellationToken cancellationToken);
    Task<ResponseE<FrequentResponseDTO>?> UpdateFrequentResponseAsync(
        long id, FrequentResponseRequestDTO request, CancellationToken cancellationToken);
    Task<ResponseE<bool>?> SetFrequentResponseActiveAsync(
        long id, bool isActive, CancellationToken cancellationToken);
    Task DeleteFrequentResponseAsync(long id, CancellationToken cancellationToken);
}

public sealed class WhatsAppBotApiClient(HttpClient httpClient, IAdminSession adminSession) : IWhatsAppBotApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public async Task<ResponseE<AuthResponseDTO>?> LoginAsync(
        LoginRequestDTO request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/v1/admin/auth/login", request, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ResponseE<AuthResponseDTO>>(
            JsonOptions, cancellationToken);
    }

    public Task<ResponseE<BotConfigurationDTO>?> GetBotConfigurationAsync(
        CancellationToken cancellationToken) =>
        GetAsync<BotConfigurationDTO>("api/v1/admin/whatsapp/bot-configuration", cancellationToken);

    public Task<ResponseE<BotConfigurationDTO>?> UpdateBotConfigurationAsync(
        UpdateBotConfigurationRequestDTO request, CancellationToken cancellationToken) =>
        SendAsync<BotConfigurationDTO>(
            HttpMethod.Put, "api/v1/admin/whatsapp/bot-configuration", request, cancellationToken);

    public Task<ResponseE<PagedResultDTO<FrequentResponseDTO>>?> ListFrequentResponsesAsync(
        int page, int pageSize, CancellationToken cancellationToken) =>
        GetAsync<PagedResultDTO<FrequentResponseDTO>>(
            $"api/v1/admin/frequent-responses?page={page}&pageSize={pageSize}", cancellationToken);

    public Task<ResponseE<FrequentResponseDTO>?> GetFrequentResponseAsync(
        long id, CancellationToken cancellationToken) =>
        GetAsync<FrequentResponseDTO>($"api/v1/admin/frequent-responses/{id}", cancellationToken);

    public Task<ResponseE<FrequentResponseDTO>?> CreateFrequentResponseAsync(
        FrequentResponseRequestDTO request, CancellationToken cancellationToken) =>
        SendAsync<FrequentResponseDTO>(
            HttpMethod.Post, "api/v1/admin/frequent-responses", request, cancellationToken);

    public Task<ResponseE<FrequentResponseDTO>?> UpdateFrequentResponseAsync(
        long id, FrequentResponseRequestDTO request, CancellationToken cancellationToken) =>
        SendAsync<FrequentResponseDTO>(
            HttpMethod.Put, $"api/v1/admin/frequent-responses/{id}", request, cancellationToken);

    public Task<ResponseE<bool>?> SetFrequentResponseActiveAsync(
        long id, bool isActive, CancellationToken cancellationToken) =>
        SendAsync<bool>(
            HttpMethod.Patch, $"api/v1/admin/frequent-responses/{id}/active",
            new FrequentResponseStateDTO { IsActive = isActive }, cancellationToken);

    public async Task DeleteFrequentResponseAsync(long id, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Delete, $"api/v1/admin/frequent-responses/{id}");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException("The administrator session is no longer authorized.", null, response.StatusCode);
        }
        var envelope = await ReadEnvelopeAsync<object?>(response, cancellationToken);
        throw new AdminApiException(envelope?.Error?.Message ?? "No se pudo eliminar la respuesta frecuente.");
    }

    private async Task<ResponseE<T>?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, path);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadEnvelopeAsync<T>(response, cancellationToken);
    }

    private async Task<ResponseE<T>?> SendAsync<T>(
        HttpMethod method, string path, object request, CancellationToken cancellationToken)
    {
        using var message = CreateAuthorizedRequest(method, path);
        message.Content = JsonContent.Create(request, options: JsonOptions);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        return await ReadEnvelopeAsync<T>(response, cancellationToken);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string path)
    {
        var message = new HttpRequestMessage(method, path);
        var accessToken = adminSession.GetApiAccessToken();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return message;
    }

    private static async Task<ResponseE<T>?> ReadEnvelopeAsync<T>(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new HttpRequestException("The administrator session is no longer authorized.", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<ResponseE<T>>(
            JsonOptions, cancellationToken);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed class AdminApiException(string message) : Exception(message);
