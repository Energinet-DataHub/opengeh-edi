create table [dbo].[PendingMessagesForAggregatedMeasureDataProcess]
(
    Id uniqueidentifier not null,
    MeteringPointType nvarchar(50) not null,
    SettlementType nvarchar(50) null,
    SettlementVersion nvarchar(50) null,
    BusinessReason nvarchar(50) not null,
    MeasureUnitType nvarchar(50) not null,
    StartPeriod datetime2(7) not null,
    EndPeriod datetime2(7) not null,
    Resolution nvarchar(50) not null,
    EnergySupplierId nvarchar(16) null,
    BalanceResponsibleId nvarchar(16) null,
    GridAreaCode nvarchar(3) not null,
    GridAreaResponsibleId nvarchar(16) not null,
    OriginalTransactionIdReference nvarchar(36) null,
    Receiver nvarchar(16) null,
    ReceiverRole nvarchar(50) null,
    Points nvarchar(max) null,
    ProcessId uniqueidentifier not null,
    PRIMARY KEY (Id),
    FOREIGN KEY (ProcessId) REFERENCES [dbo].[AggregatedMeasureDataProcesses] (ProcessId)
)