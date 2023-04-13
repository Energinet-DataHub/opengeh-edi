EXEC sp_rename '[dbo].[OutgoingMessages].[MessageType]','DocumentType', 'COLUMN'
EXEC sp_rename '[dbo].[EnqueuedMessages].[MessageType]', 'DocumentType', 'COLUMN'