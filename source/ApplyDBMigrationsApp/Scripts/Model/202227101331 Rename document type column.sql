EXEC sp_rename '[b2b].[OutgoingMessages].[DocumentType]','MessageType', 'COLUMN'
EXEC sp_rename '[b2b].[EnqueuedMessages].[DocumentType]', 'MessageType', 'COLUMN'