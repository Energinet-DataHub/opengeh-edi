BEGIN
    CREATE TABLE [dbo].[AggregatedMeasureDataProcesses]
    (
        [RecordId]                                      [int] IDENTITY (1,1) NOT NULL,
        [ProcessId]                                     [nvarchar](50)       NOT NULL,
        [BusinessTransactionId]                         [nvarchar](50)       NOT NULL,
        [SettlementVersion]                             [nvarchar](50)       NULL,
        [MarketEvaluationPointType]                     [nvarchar](50)       NULL,
        [MarketEvaluationSettlementMethod]              [nvarchar](50)       NULL,
        [StartDateAndOrTimeDateTime]                    [datetime2](7)       NOT NULL,
        [EndDateAndOrTimeDateTime]                      [datetime2](7)       NULL,
        [MeteringGridAreaDomainId]                      [nvarchar](50)       NULL,
        [EnergySupplierMarketParticipantId]             [nvarchar](50)       NULL,
        [BalanceResponsiblePartyMarketParticipantId]    [nvarchar](50)       NULL,
        [RequestedByActorId]                            [nvarchar](50)       NOT NULL,
        CONSTRAINT [PK_AggregatedMeasureDataProcesses] PRIMARY KEY NONCLUSTERED
            (
             [ProcessId] ASC
                ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY];
END
