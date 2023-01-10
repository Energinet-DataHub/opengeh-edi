DROP TABLE [B2B].[BundleStore]
CREATE TABLE [B2B].[ReadyMessages]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [Id]                               [uniqueIdentifier]   NOT NULL,
    [ReceiverNumber]                   [nvarchar](50)       NOT NULL,
    [MessageCategory]                  [nvarchar](50)       NOT NULL,
    [MessageIdsIncluded]               [nvarchar](max)      NOT NULL,
    [GeneratedDocument]                [varbinary](max)     NOT NULL,
    CONSTRAINT [PK_Id] PRIMARY KEY NONCLUSTERED
    (
        [Id]
    ) ON [PRIMARY],
    CONSTRAINT [UC_ReceiverNumber_MessageCategory] UNIQUE NONCLUSTERED
    (
        [ReceiverNumber],
        [MessageCategory]
    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
);