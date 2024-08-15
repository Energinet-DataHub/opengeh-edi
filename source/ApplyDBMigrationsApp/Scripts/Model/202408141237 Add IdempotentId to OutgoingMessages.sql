ALTER TABLE [dbo].[OutgoingMessages]
    ADD [IdempotentId] INT NOT NULL DEFAULT(0);
GO

CREATE UNIQUE INDEX IDX_OutgoingMessageIdempotency ON [dbo].[OutgoingMessages] (IdempotentId);