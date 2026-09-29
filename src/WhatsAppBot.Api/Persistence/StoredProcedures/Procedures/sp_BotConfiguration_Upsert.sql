:ON ERROR EXIT
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
USE [WhatsAppBot];
GO
CREATE OR ALTER PROCEDURE dbo.sp_BotConfiguration_Upsert
    @IntegrationId uniqueidentifier,
    @IsBotEnabled bit,
    @ReplyMode nvarchar(32)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ReplyMode NOT IN (N'FaqOnly', N'FaqThenAi', N'AiOnly')
        THROW 51011, 'The selected reply mode is not supported.', 1;

    UPDATE dbo.BotConfiguration
    SET IsBotEnabled = @IsBotEnabled,
        ReplyMode = @ReplyMode,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE IntegrationId = @IntegrationId;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT dbo.BotConfiguration (IntegrationId, IsBotEnabled, ReplyMode)
        VALUES (@IntegrationId, @IsBotEnabled, @ReplyMode);
    END;

    SELECT IntegrationId, IsBotEnabled, ReplyMode, CreatedAtUtc, UpdatedAtUtc
    FROM dbo.BotConfiguration
    WHERE IntegrationId = @IntegrationId;
END;
GO
