ALTER TABLE [dbo].[OutgoingMessages]
    Add [ReceiverNumber] [nvarchar](16) NULL;
ALTER TABLE [dbo].[OutgoingMessages]
    Add [ReceiverRole] [nvarchar](3) NULL;

go

UPDATE [dbo].[OutgoingMessages]
SET [ReceiverNumber] = [DocumentReceiverNumber]
WHERE [ReceiverNumber] IS NULL;

UPDATE [dbo].[OutgoingMessages]
SET [ReceiverRole] = [DocumentReceiverRole]
WHERE [ReceiverRole] IS NULL;

go

ALTER TABLE [dbo].[OutgoingMessages]
ALTER COLUMN [ReceiverNumber] [nvarchar](16) NOT NULL;

ALTER TABLE [dbo].[OutgoingMessages]
ALTER COLUMN [ReceiverRole] [nvarchar](3) NOT NULL;