CREATE TABLE [dbo].[IncomingMessages]
(
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [MessageId] [nvarchar](50) NOT NULL,
    [MessageType] [nvarchar](255) NOT NULL,
    CONSTRAINT [PK_IncomingMessages] PRIMARY KEY NONCLUSTERED ([MessageId] ASC, [MessageType] ASC))

CREATE UNIQUE CLUSTERED INDEX CIX_IncomingMessages ON [dbo].[IncomingMessages]([RecordId])