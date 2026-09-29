SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_WebhookInbox_Insert
    @EventKey nvarchar(256),
    @EventType nvarchar(80),
    @PhoneNumberId nvarchar(64),
    @NormalizedEventJson nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @ExistingId bigint;
    SELECT @ExistingId = WebhookInboxEventId
    FROM dbo.WebhookInboxEvent WITH (UPDLOCK, SERIALIZABLE)
    WHERE EventKey = @EventKey;
    IF @ExistingId IS NOT NULL
    BEGIN
        COMMIT TRANSACTION;
        SELECT CONVERT(bit, 0) AS Inserted;
        RETURN;
    END;

    DECLARE @IntegrationId uniqueidentifier =
        (SELECT TOP (1) IntegrationId
         FROM dbo.WhatsAppIntegration
         WHERE PhoneNumberId = @PhoneNumberId AND IsActive = 1 AND ConnectionState = N'Connected');
    IF @IntegrationId IS NULL
    BEGIN
        COMMIT TRANSACTION;
        SELECT CONVERT(bit, 0) AS Inserted;
        RETURN;
    END;
    INSERT dbo.WebhookInboxEvent (IntegrationId, EventKey, EventType, NormalizedEventJson)
    VALUES (@IntegrationId, @EventKey, @EventType, @NormalizedEventJson);
    COMMIT TRANSACTION;
    SELECT CONVERT(bit, 1) AS Inserted;
END;
GO
