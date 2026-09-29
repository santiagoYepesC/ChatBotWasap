:ON ERROR EXIT
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
USE [WhatsAppBot];
GO

IF TYPE_ID(N'dbo.FrequentResponseExpressionTableType') IS NULL
    EXEC(N'CREATE TYPE dbo.FrequentResponseExpressionTableType AS TABLE
    (
        ExpressionText nvarchar(500) NOT NULL,
        NormalizedExpression nvarchar(500) NOT NULL
    )');
GO
