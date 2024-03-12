CREATE TABLE [dbo].[WholesaleServicesProcesses]
(
    [RecordId]                           [int] IDENTITY(1,1)    NOT NULL,
    [ProcessId]                          [uniqueidentifier]     NOT NULL,
    [BusinessTransactionId]              [nvarchar](36)         NOT NULL,
    [StartOfPeriod]                      [varchar](32)          NOT NULL,
    [EndOfPeriod]                        [varchar](32)          NULL,
    [GridAreaCode]                       [nvarchar](16)         NULL,
    [ChargeOwner]                        [nvarchar](16)         NULL,
    [Resolution]                         [nvarchar](8)          NULL,
    [EnergySupplierId]                   [nvarchar](16)         NULL,
    [BusinessReason]                     [nvarchar](3)          NULL,
    [RequestedByActorId]                 [nvarchar](16)         NOT NULL,
    [RequestedByActorRoleCode]           [nvarchar](3)          NULL,
    [State]                              [nvarchar](16)         NOT NULL DEFAULT 'Initialized',
    [SettlementVersion]                  [nvarchar](3)          NULL,
    [InitiatedByMessageId]               [nvarchar](36)         NOT NULL,
    CONSTRAINT [PK_WholesaleServicesProcesses] PRIMARY KEY CLUSTERED ([ProcessId] ASC) ON [PRIMARY]
)
