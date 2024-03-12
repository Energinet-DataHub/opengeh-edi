CREATE TABLE [dbo].[WholesaleServicesProcessChargeTypes]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [ChargeTypeId]                     [uniqueidentifier]   NOT NULL,
    [WholesaleServicesProcessId]       [uniqueidentifier]   NOT NULL,
    [Id]                               [nvarchar](16)       NULL,
    [Type]                             [nvarchar](3)        NULL,
    CONSTRAINT [PK_WholesaleServicesProcessChargeTypes] PRIMARY KEY CLUSTERED ([ChargeTypeId] ASC) ON [PRIMARY],
    CONSTRAINT [FK_WholesaleServicesProcessId] FOREIGN KEY ([WholesaleServicesProcessId]) REFERENCES [dbo].[WholesaleServicesProcesses] ([ProcessId])
)   