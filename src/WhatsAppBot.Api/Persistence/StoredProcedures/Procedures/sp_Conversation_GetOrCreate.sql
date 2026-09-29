SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Conversation_GetOrCreate
    @IntegrationId uniqueidentifier,
    @ContactId bigint,
    @LastMessageAtUtc datetime2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @ConversationId bigint;
    SELECT @ConversationId = ConversationId
    FROM dbo.Conversation WITH (UPDLOCK, SERIALIZABLE)
    WHERE IntegrationId = @IntegrationId AND ContactId = @ContactId AND BasicStatus = N'Open';
    IF @ConversationId IS NULL
    BEGIN
        INSERT dbo.Conversation (IntegrationId, ContactId, BasicStatus, LastMessageAtUtc)
        VALUES (@IntegrationId, @ContactId, N'Open', @LastMessageAtUtc);
        SET @ConversationId = CONVERT(bigint, SCOPE_IDENTITY());
    END
    ELSE
    BEGIN
        UPDATE dbo.Conversation
        SET LastMessageAtUtc = CASE WHEN LastMessageAtUtc < @LastMessageAtUtc
                                    THEN @LastMessageAtUtc ELSE LastMessageAtUtc END
        WHERE ConversationId = @ConversationId;
    END;
    COMMIT TRANSACTION;
    SELECT @ConversationId AS ConversationId;
END;
GO
