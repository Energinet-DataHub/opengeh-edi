CREATE TABLE [dbo].[MarketDocuments]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [Id]                               [uniqueidentifier]   NOT NULL,
    [BundleId]                         [nvarchar](100)      NOT NULL,
    [Payload]                          [varbinary](MAX)     NOT NULL,
    CONSTRAINT [PK_MarketDocuments] PRIMARY KEY NONCLUSTERED
        ([Id] ASC) ON [PRIMARY]
) ON [PRIMARY];