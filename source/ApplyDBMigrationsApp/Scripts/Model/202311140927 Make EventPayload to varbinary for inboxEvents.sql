ALTER TABLE [dbo].[ReceivedInboxEvents] DROP COLUMN [EventPayload]
ALTER TABLE [dbo].[ReceivedInboxEvents] ADD EventPayload VARBINARY(MAX) NULL