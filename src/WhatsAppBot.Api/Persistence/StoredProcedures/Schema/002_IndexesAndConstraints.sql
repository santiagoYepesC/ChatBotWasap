:ON ERROR EXIT
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO
USE [WhatsAppBot];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Administrator') AND name = N'UX_Administrator_NormalizedEmail')
    CREATE UNIQUE INDEX UX_Administrator_NormalizedEmail ON dbo.Administrator(NormalizedEmail);
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.WhatsAppIntegration')
      AND name = N'UX_WhatsAppIntegration_ActiveSlot'
      AND filter_definition IS NULL
)
    DROP INDEX UX_WhatsAppIntegration_ActiveSlot ON dbo.WhatsAppIntegration;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WhatsAppIntegration') AND name = N'UX_WhatsAppIntegration_ActiveSlot')
    CREATE UNIQUE INDEX UX_WhatsAppIntegration_ActiveSlot ON dbo.WhatsAppIntegration(ActiveSlot)
        WHERE IsActive = 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WhatsAppIntegration') AND name = N'UX_WhatsAppIntegration_PhoneNumberId_Active')
    CREATE UNIQUE INDEX UX_WhatsAppIntegration_PhoneNumberId_Active ON dbo.WhatsAppIntegration(PhoneNumberId)
        WHERE PhoneNumberId IS NOT NULL AND IsActive = 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FrequentResponse') AND name = N'IX_FrequentResponse_ActivePriority')
    CREATE INDEX IX_FrequentResponse_ActivePriority ON dbo.FrequentResponse(IntegrationId, IsActive, Priority DESC, FrequentResponseId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FrequentResponseExpression') AND name = N'UX_FrequentResponseExpression_Normalized')
    CREATE UNIQUE INDEX UX_FrequentResponseExpression_Normalized ON dbo.FrequentResponseExpression(FrequentResponseId, NormalizedExpression);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Contact') AND name = N'UX_Contact_Integration_User')
    CREATE UNIQUE INDEX UX_Contact_Integration_User ON dbo.Contact(IntegrationId, WhatsAppUserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Conversation') AND name = N'UX_Conversation_Open')
    CREATE UNIQUE INDEX UX_Conversation_Open ON dbo.Conversation(IntegrationId, ContactId) WHERE BasicStatus = N'Open';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Conversation') AND name = N'IX_Conversation_LastMessage')
    CREATE INDEX IX_Conversation_LastMessage ON dbo.Conversation(IntegrationId, LastMessageAtUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Message') AND name = N'UX_Message_ProviderId')
    CREATE UNIQUE INDEX UX_Message_ProviderId ON dbo.Message(IntegrationId, ProviderMessageId) WHERE ProviderMessageId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Message') AND name = N'IX_Message_ConversationChronology')
    CREATE INDEX IX_Message_ConversationChronology ON dbo.Message(ConversationId, CreatedAtUtc, MessageId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MediaAttachment') AND name = N'UX_MediaAttachment_Message')
    CREATE UNIQUE INDEX UX_MediaAttachment_Message ON dbo.MediaAttachment(MessageId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WebhookInboxEvent') AND name = N'UX_WebhookInbox_EventKey')
    CREATE UNIQUE INDEX UX_WebhookInbox_EventKey ON dbo.WebhookInboxEvent(EventKey);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WebhookInboxEvent') AND name = N'IX_WebhookInbox_State')
    CREATE INDEX IX_WebhookInbox_State ON dbo.WebhookInboxEvent(State, ReceivedAtUtc);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.MessageOutbox') AND name = N'IX_MessageOutbox_State')
    CREATE INDEX IX_MessageOutbox_State ON dbo.MessageOutbox(State, NextAttemptAtUtc);
GO
