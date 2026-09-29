using System.Data;
using Microsoft.Data.SqlClient;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Entities;
using WhatsAppBot.Shared.Enums;

namespace WhatsAppBot.Api.Persistence.Repositories;

public sealed class BotConfigurationRepository(StoredProcedureExecutor executor) : IBotConfigurationRepository
{
    public async Task<BotConfiguration> GetCurrentAsync(CancellationToken cancellationToken) =>
        await executor.QuerySingleAsync(
            "dbo.sp_BotConfiguration_Get",
            [],
            Map,
            cancellationToken)
        ?? throw new InvalidOperationException("The initial bot configuration has not been provisioned.");

    public async Task<BotConfiguration> UpdateAsync(
        Guid integrationId, bool isBotEnabled, BotReplyMode replyMode, CancellationToken cancellationToken) =>
        await executor.QuerySingleAsync(
            "dbo.sp_BotConfiguration_Upsert",
            [
                new SqlParameter("@IntegrationId", SqlDbType.UniqueIdentifier) { Value = integrationId },
                new SqlParameter("@IsBotEnabled", SqlDbType.Bit) { Value = isBotEnabled },
                new SqlParameter("@ReplyMode", SqlDbType.NVarChar, 32) { Value = replyMode.ToString() }
            ],
            Map,
            cancellationToken)
        ?? throw new InvalidOperationException("The bot configuration could not be updated.");

    private static BotConfiguration Map(SqlDataReader reader) => new(
        reader.GetGuid(reader.GetOrdinal("IntegrationId")),
        reader.GetBoolean(reader.GetOrdinal("IsBotEnabled")),
        Enum.Parse<BotReplyMode>(reader.GetString(reader.GetOrdinal("ReplyMode")), ignoreCase: false))
    {
        CreatedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(
            reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")), DateTimeKind.Utc)),
        UpdatedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(
            reader.GetDateTime(reader.GetOrdinal("UpdatedAtUtc")), DateTimeKind.Utc))
    };
}
