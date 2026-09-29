using Microsoft.Data.SqlClient;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Contracts;
using WhatsAppBot.Shared.Enums;

namespace WhatsAppBot.Api.Business.Workers;

public sealed class MessageOutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<MessageOutboxWorker> logger) : BackgroundService
{
    private const int MaximumAttempts = 5;

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
                    "Message outbox persistence failed with SQL error number {SqlErrorNumber}.",
                    exception.Number);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var messages = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<IMetaWhatsAppClient>();
        var policy = scope.ServiceProvider.GetRequiredService<IWhatsAppMessagingPolicy>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var batch = await messages.ClaimOutboxBatchAsync(20, cancellationToken);

        foreach (var item in batch)
        {
            try
            {
                var decision = policy.Evaluate(item.LastCustomerMessageAtUtc, clock.UtcNow);
                var result = decision.IsAllowed
                    ? await sender.SendTextAsync(item.PhoneNumberId, item.Recipient, item.Text, cancellationToken)
                    : new MetaMessageSendResult(false, null, decision.OutcomeCode, false);
                var terminal = result.Succeeded || !result.IsTransient || item.AttemptCount >= MaximumAttempts;
                await messages.SetOutboxResultAsync(item.MessageId, result, terminal, cancellationToken);
                if (!terminal)
                {
                    var delay = Math.Min(3600, 5 * (1 << Math.Min(item.AttemptCount - 1, 9)));
                    await messages.SetOutboxStateAsync(
                        item.MessageId, MessageOutboxState.Pending, delay, cancellationToken);
                }
            }
            catch (SqlException exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(
                    "Outbound message persistence failed with SQL error number {SqlErrorNumber}.",
                    exception.Number);
            }
        }
    }
}
