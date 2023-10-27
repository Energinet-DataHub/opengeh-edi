create table [dbo].[PendingAggregations]
(
    Id uniqueidentifier not null,
    MeteringPointType nvarchar(3) not null,
    SettlementType nvarchar(3) null,
    SettlementVersion nvarchar(3) null,
    BusinessReason nvarchar(3) not null,
    MeasurementUnit nvarchar(3) not null,
    PeriodStart datetime2(7) not null,
    PeriodEnd datetime2(7) not null,
    Resolution nvarchar(50) not null,
    EnergySupplierId nvarchar(16) null,
    BalanceResponsibleId nvarchar(16) null,
    GridAreaCode nvarchar(3) not null,
    GridAreaOwnerId nvarchar(16) not null,
    BusinessTransactionId nvarchar(36) null,
    ReceiverId nvarchar(16) null,
    ReceiverRole nvarchar(3) null,
    Points nvarchar(max) null,
    ProcessId uniqueidentifier not null,
    PRIMARY KEY (Id),
    FOREIGN KEY (ProcessId) REFERENCES [dbo].[AggregatedMeasureDataProcesses] (ProcessId)
)