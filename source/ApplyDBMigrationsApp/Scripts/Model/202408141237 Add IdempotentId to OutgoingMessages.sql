ALTER TABLE [dbo].[OutgoingMessages]
    ADD [IdempotentId] VARCHAR(60) NULL;
GO

UPDATE [dbo].[OutgoingMessages]
SET [IdempotentId] = [ExternalId]
WHERE [IdempotentId] IS NULL;
GO

ALTER TABLE [dbo].[OutgoingMessages]
ALTER COLUMN [IdempotentId] VARCHAR(60) NOT NULL;
GO

-- recreate the index with the new IdempotentId column
DROP INDEX [dbo].[OutgoingMessages].IDX_OutgoingMessageIdempotency;
CREATE UNIQUE INDEX IDX_OutgoingMessageIdempotency ON [dbo].[OutgoingMessages] (IdempotentId);