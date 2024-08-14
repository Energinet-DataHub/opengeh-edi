ALTER TABLE [dbo].[OutgoingMessages]
    ADD [IdempotentId] varchar(255) NULL; 
GO

UPDATE [dbo].[OutgoingMessages]
SET [IdempotentId] = [ExternalId]
WHERE [IdempotentId] IS NULL;
GO

ALTER TABLE [dbo].[OutgoingMessages]
    ALTER COLUMN [IdempotentId] varchar(255) NOT NULL; 
GO

CREATE UNIQUE INDEX IDX_OutgoingMessageIdempotency ON [dbo].[OutgoingMessages] (IdempotentId);
