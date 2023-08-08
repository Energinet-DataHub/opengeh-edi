ALTER TABLE [dbo].[ReceivedInboxEvents] ADD ProcessedDate datetime2(7)   NULL
ALTER TABLE [dbo].[ReceivedInboxEvents] ADD ErrorMessage nvarchar(MAX)   NULL
GO