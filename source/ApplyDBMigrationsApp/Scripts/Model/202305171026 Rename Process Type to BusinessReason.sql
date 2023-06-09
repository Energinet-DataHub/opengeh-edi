EXEC sp_rename '[dbo].[ArchivedMessages].[ProcessType]','BusinessReason', 'COLUMN'
EXEC sp_rename '[dbo].[OutgoingMessages].[ProcessType]','BusinessReason', 'COLUMN'
EXEC sp_rename '[dbo].[EnqueuedMessages].[ProcessType]','BusinessReason', 'COLUMN'