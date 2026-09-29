SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_WhatsAppIntegration_Disconnect
    @IntegrationId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.WhatsAppIntegration
    SET WabaId = NULL,
        PhoneNumberId = NULL,
        BusinessPhoneNumber = NULL,
        DisplayName = NULL,
        ConnectionState = N'NotConnected',
        ConnectedAtUtc = NULL,
        LastConnectionErrorCode = NULL,
        AccessTokenSecretRef = NULL,
        MetaAppSecretRef = NULL,
        WebhookVerifyTokenRef = NULL,
        IsActive = 0,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE IntegrationId = @IntegrationId;
    UPDATE dbo.BotConfiguration
    SET IsBotEnabled = 0, UpdatedAtUtc = SYSUTCDATETIME()
    WHERE IntegrationId = @IntegrationId;
    COMMIT TRANSACTION;
END;
GO
