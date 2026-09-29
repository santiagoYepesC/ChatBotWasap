SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Message_CreateOutboundPending
    @InboundMessageId bigint,
    @IntegrationId uniqueidentifier,
    @ConversationId bigint,
    @ContentText nvarchar(max),
    @ReplySource nvarchar(32),
    @FrequentResponseId bigint = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @ExistingOutboundMessageId bigint;
    SELECT @ExistingOutboundMessageId = MessageId
    FROM dbo.MessageOutbox WITH (UPDLOCK, SERIALIZABLE)
    WHERE SourceInboundMessageId = @InboundMessageId;
    IF @ExistingOutboundMessageId IS NOT NULL
    BEGIN
        COMMIT TRANSACTION;
        SELECT CONVERT(bit, 0) AS Inserted, @ExistingOutboundMessageId AS MessageId,
               CONVERT(nvarchar(100), NULL) AS OutcomeCode;
        RETURN;
    END;

    DECLARE @LastCustomerMessageAtUtc datetime2(3);
    SELECT @LastCustomerMessageAtUtc = MAX(ProviderTimestampUtc)
    FROM dbo.Message WITH (UPDLOCK, HOLDLOCK)
    WHERE IntegrationId = @IntegrationId
      AND ConversationId = @ConversationId
      AND Direction = N'Inbound';

    IF NOT EXISTS
       (SELECT 1 FROM dbo.Message
        WHERE MessageId = @InboundMessageId
          AND IntegrationId = @IntegrationId
          AND ConversationId = @ConversationId
          AND Direction = N'Inbound')
       OR NOT EXISTS
       (SELECT 1 FROM dbo.WhatsAppIntegration
        WHERE IntegrationId = @IntegrationId AND IsActive = 1 AND ConnectionState = N'Connected')
       OR @LastCustomerMessageAtUtc IS NULL
       OR DATEDIFF_BIG(second, @LastCustomerMessageAtUtc, SYSUTCDATETIME()) NOT BETWEEN 0 AND 86399
    BEGIN
        COMMIT TRANSACTION;
        SELECT CONVERT(bit, 0) AS Inserted, CONVERT(bigint, NULL) AS MessageId,
               N'MessagingWindowClosed' AS OutcomeCode;
        RETURN;
    END;

    INSERT dbo.Message
        (IntegrationId, ConversationId, Direction, MessageType, ContentText, ReplySource,
         FrequentResponseId, ProcessingState, DeliveryState, ProviderTimestampUtc)
    VALUES
        (@IntegrationId, @ConversationId, N'Outbound', N'Text', @ContentText, @ReplySource,
         @FrequentResponseId, N'Processing', N'Pending', SYSUTCDATETIME());
    DECLARE @OutboundMessageId bigint = CONVERT(bigint, SCOPE_IDENTITY());
    INSERT dbo.MessageOutbox (MessageId, IntegrationId, SourceInboundMessageId)
    VALUES (@OutboundMessageId, @IntegrationId, @InboundMessageId);
    UPDATE dbo.Message
    SET ProcessingState = N'Completed', UpdatedAtUtc = SYSUTCDATETIME()
    WHERE MessageId = @InboundMessageId;

    COMMIT TRANSACTION;
    SELECT CONVERT(bit, 1) AS Inserted, @OutboundMessageId AS MessageId, CONVERT(nvarchar(100), NULL) AS OutcomeCode;
END;
GO
