ALTER TABLE [dbo].[OutgoingMessages]
    ADD [ResultId] UNIQUEIDENTIFIER NULL; 
GO

UPDATE [dbo].[OutgoingMessages]
SET [ResultId] = [Id]
WHERE [ResultId] IS NULL;
GO

ALTER TABLE [dbo].[OutgoingMessages]
    ADD [ResultId] UNIQUEIDENTIFIER NOT NULL; 
GO

CREATE INDEX IX_OutgoingMessageIdempotency ON [dbo].[OutgoingMessages] (ResultId, ReceiverNumber, ReceiverRole);
