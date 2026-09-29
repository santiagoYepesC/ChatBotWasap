:ON ERROR EXIT
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
USE [WhatsAppBot];
GO
CREATE OR ALTER PROCEDURE dbo.sp_Admin_GetByEmailForAuthentication
    @Email nvarchar(320)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT AdminId, NormalizedEmail AS Email, PasswordHash, IsActive
    FROM dbo.Administrator
    WHERE NormalizedEmail = UPPER(LTRIM(RTRIM(@Email)));
END;
GO
