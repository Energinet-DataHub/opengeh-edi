BEGIN
    CREATE TABLE [dbo].[AggregatedMeasureDataProcesses]
    (
        [RecordId]                                      [int] IDENTITY (1,1) NOT NULL,
        [ProcessId]                                     [nvarchar](36)       NOT NULL,
        [BusinessTransactionId]                         [nvarchar](36)       NOT NULL,
        [SettlementVersion]                             [nvarchar](3)        NULL,
        [MeteringPointType]                             [nvarchar](8)        NULL,
        [SettlementMethod]                              [nvarchar](8)        NULL,
        [StartOfPeriod]                                 [datetime2](7)       NOT NULL,
        [EndOfPeriod]                                   [datetime2](7)       NULL,
        [MeteringGridAreaDomainId]                      [nvarchar](16)       NULL, 
        [EnergySupplierId]                              [nvarchar](16)       NULL,
        [BalanceResponsibleId]                          [nvarchar](16)       NULL,
        [RequestedByActorId]                            [nvarchar](16)       NOT NULL,
        CONSTRAINT [PK_AggregatedMeasureDataProcesses] PRIMARY KEY NONCLUSTERED
            (
             [ProcessId] ASC
                ) WITH (STATISTICS_NORECOMPUTE = OFF) ON [PRIMARY]
    ) ON [PRIMARY];
END
