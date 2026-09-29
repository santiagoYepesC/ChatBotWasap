:ON ERROR EXIT
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
USE [WhatsAppBot];
GO
CREATE OR ALTER PROCEDURE dbo.sp_Admin_UpdateLastLogin
    @AdminId bigint
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Administrator
    SET LastLoginAtUtc = SYSUTCDATETIME()
    WHERE AdminId = @AdminId AND IsActive = 1;
END;
GO
