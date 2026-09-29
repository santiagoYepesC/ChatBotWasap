:ON ERROR EXIT
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
USE [WhatsAppBot];
GO
CREATE OR ALTER PROCEDURE dbo.sp_BotConfiguration_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        IntegrationId, IsBotEnabled, ReplyMode, CreatedAtUtc, UpdatedAtUtc
    FROM dbo.BotConfiguration
    ORDER BY CreatedAtUtc, IntegrationId;

    IF @@ROWCOUNT = 0
        THROW 51010, 'The initial bot configuration has not been provisioned.', 1;
END;
GO
