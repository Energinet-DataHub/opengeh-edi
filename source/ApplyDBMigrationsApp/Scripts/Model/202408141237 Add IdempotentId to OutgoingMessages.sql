ALTER TABLE [dbo].[OutgoingMessages]
    ADD [IdempotentId] BINARY(32) NOT NULL DEFAULT 0x0000000000000000000000000000000000000000000000000000000000000000;
GO

-- recreate the index with the new IdempotentId column
DROP INDEX [dbo].[OutgoingMessages].IDX_OutgoingMessageIdempotency;
CREATE UNIQUE INDEX IDX_OutgoingMessageIdempotency ON [dbo].[OutgoingMessages] (IdempotentId);