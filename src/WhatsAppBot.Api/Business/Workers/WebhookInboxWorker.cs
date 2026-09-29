using Microsoft.Data.SqlClient;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Business.UseCases;
using WhatsAppBot.Api.Models.Contracts;
using WhatsAppBot.Shared.Enums;

namespace WhatsAppBot.Api.Business.Workers;

public sealed class WebhookInboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<WebhookInboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        do
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (SqlException exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(
                    "Webhook inbox persistence failed with SQL error number {SqlErrorNumber}.",
                    exception.Number);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var inbox = scope.ServiceProvider.GetRequiredService<IWebhookInboxRepository>();
        var processor = scope.ServiceProvider.GetRequiredService<MessageProcessingService>();
        var events = await inbox.ClaimBatchAsync(20, cancellationToken);
        foreach (var webhookEvent in events)
        {
            try
            {
                await processor.ProcessAsync(webhookEvent, cancellationToken);
                await inbox.SetStateAsync(
                    webhookEvent.EventKey, WebhookInboxState.Completed, null, cancellationToken);
            }
            catch (BusinessValidationException)
            {
                await inbox.SetStateAsync(
                    webhookEvent.EventKey, WebhookInboxState.Failed, "InvalidNormalizedEvent", cancellationToken);
                logger.LogWarning(
                    "A normalized webhook event was rejected. Event type: {EventType}.",
                    webhookEvent.EventType);
            }
            catch (SqlException exception) when (!cancellationToken.IsCancellationRequested)
            {
                await inbox.SetStateAsync(
                    webhookEvent.EventKey, WebhookInboxState.Failed, "PersistenceFailure", cancellationToken);
                logger.LogError(
                    "Webhook event processing failed with SQL error number {SqlErrorNumber}.",
                    exception.Number);
            }
            catch (InvalidOperationException)
            {
                await inbox.SetStateAsync(
                    webhookEvent.EventKey, WebhookInboxState.Failed, "ProcessingFailure", cancellationToken);
                logger.LogError("Webhook event processing failed due to an invalid application state.");
            }
        }
    }
}
