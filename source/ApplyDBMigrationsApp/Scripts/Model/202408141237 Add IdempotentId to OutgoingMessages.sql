ALTER TABLE [dbo].[OutgoingMessages]
    ADD [IdempotentId] INT NOT NULL DEFAULT(0);
GO

-- recreate the index with the new IdempotentId column
DROP INDEX [dbo].[OutgoingMessages].IDX_OutgoingMessageIdempotency;
CREATE UNIQUE INDEX IDX_OutgoingMessageIdempotency ON [dbo].[OutgoingMessages] (IdempotentId);