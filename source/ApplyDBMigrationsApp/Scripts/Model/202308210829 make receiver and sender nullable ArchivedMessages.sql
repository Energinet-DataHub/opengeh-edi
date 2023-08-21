DROP INDEX IX_FindMessages ON [dbo].[ArchivedMessages]

ALTER TABLE [dbo].[ArchivedMessages]
ALTER COLUMN [ReceiverNumber] nvarchar(255) NULL

ALTER TABLE [dbo].[ArchivedMessages]
ALTER COLUMN [SenderNumber] nvarchar(255) NULL
GO

CREATE NONCLUSTERED INDEX IX_FindMessages ON [dbo].[ArchivedMessages] (ReceiverNumber, SenderNumber, MessageId)
GO