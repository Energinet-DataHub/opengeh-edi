DROP TABLE [B2B].[BundleStore]
CREATE TABLE [B2B].[BundleStore]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [ActorNumber]                      [nvarchar](50)       NOT NULL,
    [ActorRole]                        [nvarchar](50)       NOT NULL,
    [MessageCategory]                  [nvarchar](50)       NOT NULL,
    [MessageId]                        [uniqueIdentifier]   NULL,
    [Bundle]                           [varbinary](max)     NULL
    CONSTRAINT [PK_Id] PRIMARY KEY NONCLUSTERED
        (
        [ActorNumber],
        [ActorRole],
        [MessageCategory] 
        ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY];