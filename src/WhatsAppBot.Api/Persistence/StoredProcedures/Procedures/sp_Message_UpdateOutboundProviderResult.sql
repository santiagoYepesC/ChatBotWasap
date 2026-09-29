SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Message_UpdateOutboundProviderResult
    @MessageId bigint,
    @Succeeded bit,
    @ProviderMessageId nvarchar(256) = NULL,
    @SafeFailureCode nvarchar(100) = NULL,
    @IsTerminal bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    IF @Succeeded = 1
    BEGIN
        UPDATE dbo.Message
        SET ProviderMessageId = @ProviderMessageId,
            DeliveryState = N'Sent',
            ProcessingState = N'Completed',
            FailureCode = NULL,
            UpdatedAtUtc = SYSUTCDATETIME()
        WHERE MessageId = @MessageId AND Direction = N'Outbound';
        UPDATE dbo.MessageOutbox
        SET State = N'Sent', UpdatedAtUtc = SYSUTCDATETIME()
        WHERE MessageId = @MessageId;
    END
    ELSE IF @IsTerminal = 1
    BEGIN
        UPDATE dbo.Message
        SET DeliveryState = N'Failed',
            ProcessingState = N'Failed',
            FailureCode = @SafeFailureCode,
            UpdatedAtUtc = SYSUTCDATETIME()
        WHERE MessageId = @MessageId AND Direction = N'Outbound';
        UPDATE dbo.MessageOutbox
        SET State = N'Failed', UpdatedAtUtc = SYSUTCDATETIME()
        WHERE MessageId = @MessageId;
    END
    ELSE
    BEGIN
        UPDATE dbo.Message
        SET DeliveryState = N'Pending',
            ProcessingState = N'Processing',
            FailureCode = NULL,
            UpdatedAtUtc = SYSUTCDATETIME()
        WHERE MessageId = @MessageId AND Direction = N'Outbound';
    END;
    COMMIT TRANSACTION;
END;
GO
