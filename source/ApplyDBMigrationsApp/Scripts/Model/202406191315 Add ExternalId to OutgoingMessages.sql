ALTER TABLE [dbo].[OutgoingMessages]
    ADD [ExternalId] UNIQUEIDENTIFIER NULL; 
GO

UPDATE [dbo].[OutgoingMessages]
SET [ExternalId] = [Id]
WHERE [ExternalId] IS NULL;
GO

ALTER TABLE [dbo].[OutgoingMessages]
    ADD [ExternalId] UNIQUEIDENTIFIER NOT NULL; 
GO

CREATE INDEX IX_OutgoingMessageIdempotency ON [dbo].[OutgoingMessages] (ExternalId, ReceiverNumber, ReceiverRole);
