SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_WhatsAppIntegration_SaveSignupResult
    @IntegrationId uniqueidentifier,
    @WabaId nvarchar(64),
    @PhoneNumberId nvarchar(64),
    @BusinessPhoneNumber nvarchar(32),
    @DisplayName nvarchar(256) = NULL,
    @AccessTokenSecretRef nvarchar(512),
    @MetaAppSecretRef nvarchar(512),
    @WebhookVerifyTokenRef nvarchar(512)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    UPDATE dbo.WhatsAppIntegration WITH (UPDLOCK, SERIALIZABLE)
    SET WabaId = @WabaId,
        PhoneNumberId = @PhoneNumberId,
        BusinessPhoneNumber = @BusinessPhoneNumber,
        DisplayName = @DisplayName,
        ConnectionState = N'Connected',
        ConnectedAtUtc = SYSUTCDATETIME(),
        LastConnectionErrorCode = NULL,
        AccessTokenSecretRef = @AccessTokenSecretRef,
        MetaAppSecretRef = @MetaAppSecretRef,
        WebhookVerifyTokenRef = @WebhookVerifyTokenRef,
        IsActive = 1,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE IntegrationId = @IntegrationId;
    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51002, 'WhatsApp integration slot was not found.', 1;
    END;
    COMMIT TRANSACTION;
END;
GO
