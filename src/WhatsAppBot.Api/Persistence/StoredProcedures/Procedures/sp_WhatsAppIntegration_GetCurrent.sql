SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_WhatsAppIntegration_GetCurrent
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
        IntegrationId, WabaId, PhoneNumberId, BusinessPhoneNumber, DisplayName,
        ConnectionState, ConnectedAtUtc, LastConnectionErrorCode,
        AccessTokenSecretRef, MetaAppSecretRef, WebhookVerifyTokenRef,
        CreatedAtUtc, UpdatedAtUtc, IsActive
    FROM dbo.WhatsAppIntegration
    ORDER BY CASE WHEN IsActive = 1 THEN 0 ELSE 1 END, CreatedAtUtc, IntegrationId;
END;
GO
