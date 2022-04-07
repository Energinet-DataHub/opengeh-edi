CREATE SCHEMA b2b

CREATE TABLE [b2b].[MessageIds]
(
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [MessageId] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_MessageIds] PRIMARY KEY NONCLUSTERED ([MessageId] ASC))