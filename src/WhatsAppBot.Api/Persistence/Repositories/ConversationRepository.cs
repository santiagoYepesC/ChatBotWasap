using System.Data;
using Microsoft.Data.SqlClient;
using WhatsAppBot.Api.Business.Abstractions;

namespace WhatsAppBot.Api.Persistence.Repositories;

public sealed class ConversationRepository(StoredProcedureExecutor executor) : IConversationRepository
{
    public async Task<long> GetOrCreateAsync(
        Guid integrationId,
        long contactId,
        DateTimeOffset lastMessageAtUtc,
        CancellationToken cancellationToken)
    {
        var conversationId = await executor.QuerySingleAsync(
            "dbo.sp_Conversation_GetOrCreate",
            [
                new SqlParameter("@IntegrationId", SqlDbType.UniqueIdentifier) { Value = integrationId },
                new SqlParameter("@ContactId", SqlDbType.BigInt) { Value = contactId },
                new SqlParameter("@LastMessageAtUtc", SqlDbType.DateTime2)
                    { Scale = 3, Value = lastMessageAtUtc.UtcDateTime }
            ],
            reader => reader.GetInt64(reader.GetOrdinal("ConversationId")),
            cancellationToken);
        return conversationId > 0
            ? conversationId
            : throw new InvalidOperationException("The WhatsApp conversation could not be persisted.");
    }
}
