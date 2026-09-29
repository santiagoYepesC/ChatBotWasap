using System.Data;
using Microsoft.Data.SqlClient;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Contracts;
using WhatsAppBot.Shared.Enums;

namespace WhatsAppBot.Api.Persistence.Repositories;

public sealed class MessageRepository(StoredProcedureExecutor executor) : IMessageRepository
{
    public async Task<InboundMessageInsertResult> InsertInboundAsync(
        Guid integrationId,
        long conversationId,
        NormalizedMetaEvent inboundEvent,
        CancellationToken cancellationToken)
    {
        var result = await executor.QuerySingleAsync(
            "dbo.sp_Message_InsertInbound",
            [
                new SqlParameter("@IntegrationId", SqlDbType.UniqueIdentifier) { Value = integrationId },
                new SqlParameter("@ConversationId", SqlDbType.BigInt) { Value = conversationId },
                new SqlParameter("@ProviderMessageId", SqlDbType.NVarChar, 256)
                    { Value = inboundEvent.ProviderMessageId! },
                new SqlParameter("@MessageType", SqlDbType.NVarChar, 24) { Value = MessageType.Text.ToString() },
                new SqlParameter("@ContentText", SqlDbType.NVarChar, -1) { Value = inboundEvent.Text! },
                new SqlParameter("@ProviderTimestampUtc", SqlDbType.DateTime2)
                    { Scale = 3, Value = inboundEvent.ProviderTimestampUtc!.Value.UtcDateTime }
            ],
            reader => new InboundMessageInsertResult(
                reader.GetBoolean(reader.GetOrdinal("Inserted")),
                reader.GetInt64(reader.GetOrdinal("MessageId"))),
            cancellationToken);
        return result ?? throw new InvalidOperationException("The inbound WhatsApp message could not be persisted.");
    }

    public Task SetProcessingOutcomeAsync(
        long messageId,
        ProcessingState state,
        string? safeOutcomeCode,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(
            "dbo.sp_Message_SetProcessingOutcome",
            [
                new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = messageId },
                new SqlParameter("@ProcessingState", SqlDbType.NVarChar, 24) { Value = state.ToString() },
                new SqlParameter("@SafeOutcomeCode", SqlDbType.NVarChar, 100)
                    { Value = (object?)safeOutcomeCode ?? DBNull.Value }
            ],
            cancellationToken);

    public async Task<IReadOnlyList<OutboundMessageWorkItem>> ClaimOutboxBatchAsync(
        int batchSize, CancellationToken cancellationToken) =>
        (await executor.QueryAsync(
            "dbo.sp_MessageOutbox_ClaimBatch",
            [new SqlParameter("@BatchSize", SqlDbType.Int) { Value = batchSize }],
            reader => new OutboundMessageWorkItem(
                reader.GetInt64(reader.GetOrdinal("MessageId")),
                reader.GetGuid(reader.GetOrdinal("IntegrationId")),
                reader.GetString(reader.GetOrdinal("PhoneNumberId")),
                reader.GetString(reader.GetOrdinal("Recipient")),
                reader.GetString(reader.GetOrdinal("ContentText")),
                new DateTimeOffset(DateTime.SpecifyKind(
                    reader.GetDateTime(reader.GetOrdinal("LastCustomerMessageAtUtc")), DateTimeKind.Utc)),
                reader.GetInt32(reader.GetOrdinal("AttemptCount"))),
            cancellationToken)).ToArray();

    public Task SetOutboxResultAsync(
        long messageId,
        MetaMessageSendResult result,
        bool isTerminal,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(
            "dbo.sp_Message_UpdateOutboundProviderResult",
            [
                new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = messageId },
                new SqlParameter("@Succeeded", SqlDbType.Bit) { Value = result.Succeeded },
                new SqlParameter("@ProviderMessageId", SqlDbType.NVarChar, 256)
                    { Value = (object?)result.ProviderMessageId ?? DBNull.Value },
                new SqlParameter("@SafeFailureCode", SqlDbType.NVarChar, 100)
                    { Value = (object?)result.SafeFailureCode ?? DBNull.Value },
                new SqlParameter("@IsTerminal", SqlDbType.Bit) { Value = isTerminal }
            ],
            cancellationToken);

    public Task SetOutboxStateAsync(
        long messageId,
        MessageOutboxState state,
        int retryDelaySeconds,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(
            "dbo.sp_MessageOutbox_SetState",
            [
                new SqlParameter("@MessageId", SqlDbType.BigInt) { Value = messageId },
                new SqlParameter("@State", SqlDbType.NVarChar, 16) { Value = state.ToString() },
                new SqlParameter("@RetryDelaySeconds", SqlDbType.Int) { Value = retryDelaySeconds }
            ],
            cancellationToken);
}
