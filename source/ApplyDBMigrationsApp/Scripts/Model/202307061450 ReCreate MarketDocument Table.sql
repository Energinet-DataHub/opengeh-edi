DROP TABLE [dbo].[MarketDocuments];
GO

CREATE TABLE [dbo].[MarketDocuments]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [Id]                               [uniqueidentifier]   NOT NULL,
    [BundleId]                         [uniqueidentifier]   NOT NULL,
    [Payload]                          [varbinary](MAX)     NOT NULL,
    CONSTRAINT [PK_MarketDocuments] PRIMARY KEY NONCLUSTERED
        ([Id] ASC) ON [PRIMARY]
) ON [PRIMARY];