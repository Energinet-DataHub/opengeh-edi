ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ALTER COLUMN [BusinessReason] NVARCHAR(3) NOT NULL

ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ALTER COLUMN [RequestedByActorRole] NVARCHAR(3) NOT NULL

ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ALTER COLUMN [StartOfPeriod] NVARCHAR(32) NOT NULL
