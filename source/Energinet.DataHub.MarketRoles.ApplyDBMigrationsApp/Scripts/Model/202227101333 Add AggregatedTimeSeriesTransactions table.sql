CREATE TABLE [B2B].[AggregatedTimeSeriesTransactions]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [Id]                               [uniqueidentifier]      NOT NULL,
    CONSTRAINT [PK_AggregatedTimeSeriesTransactions_Id] PRIMARY KEY NONCLUSTERED
        (
        [Id] ASC
        ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY];