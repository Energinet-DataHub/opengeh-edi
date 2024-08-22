DROP INDEX [dbo].[OutgoingMessages].IDX_OutgoingMessageIdempotency;
ALTER TABLE [dbo].[OutgoingMessages] DROP COLUMN [IdempotentId]
