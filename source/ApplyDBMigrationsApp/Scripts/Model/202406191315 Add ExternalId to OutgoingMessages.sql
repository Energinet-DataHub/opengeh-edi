ALTER TABLE [dbo].[OutgoingMessages]
    ADD [ExternalId] UNIQUEIDENTIFIER NULL; 
GO

UPDATE [dbo].[OutgoingMessages]
SET [ExternalId] = [Id]
WHERE [ExternalId] IS NULL;
GO

ALTER TABLE [dbo].[OutgoingMessages]
    ALTER COLUMN [ExternalId] UNIQUEIDENTIFIER NOT NULL; 
GO

CREATE UNIQUE INDEX IDX_OutgoingMessageIdempotency ON [dbo].[OutgoingMessages] (ExternalId, ReceiverNumber, ReceiverRole);
