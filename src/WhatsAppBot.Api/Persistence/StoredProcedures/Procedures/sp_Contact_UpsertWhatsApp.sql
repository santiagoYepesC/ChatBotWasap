SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Contact_UpsertWhatsApp
    @IntegrationId uniqueidentifier,
    @WhatsAppUserId nvarchar(64),
    @DisplayName nvarchar(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @ContactId bigint;
    SELECT @ContactId = ContactId
    FROM dbo.Contact WITH (UPDLOCK, SERIALIZABLE)
    WHERE IntegrationId = @IntegrationId AND WhatsAppUserId = @WhatsAppUserId;
    IF @ContactId IS NULL
    BEGIN
        INSERT dbo.Contact (IntegrationId, WhatsAppUserId, DisplayName)
        VALUES (@IntegrationId, @WhatsAppUserId, @DisplayName);
        SET @ContactId = CONVERT(bigint, SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        UPDATE dbo.Contact
        SET DisplayName = COALESCE(@DisplayName, DisplayName),
            UpdatedAtUtc = SYSUTCDATETIME()
        WHERE ContactId = @ContactId;
    END;
    COMMIT TRANSACTION;
    SELECT @ContactId AS ContactId;
END;
GO
