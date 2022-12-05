CREATE TABLE [B2B].[BundleStore]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [Id]                               [nvarchar](100)      NOT NULL,
    [Bundle]                           [varbinary](max)     NULL
    CONSTRAINT [PK_Id] PRIMARY KEY NONCLUSTERED
        (
        [Id] ASC
        ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY];