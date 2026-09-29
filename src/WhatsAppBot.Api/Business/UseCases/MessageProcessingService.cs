using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Contracts;

namespace WhatsAppBot.Api.Business.UseCases;

public sealed class MessageProcessingService(IProcessInboundText inboundTextProcessor)
{
    public Task ProcessAsync(NormalizedMetaEvent webhookEvent, CancellationToken cancellationToken) =>
        webhookEvent.EventType == MetaWebhookEventTypes.InboundText
            ? inboundTextProcessor.ProcessAsync(webhookEvent, cancellationToken)
            : Task.CompletedTask;
}
