ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ADD [OriginalActorNumber] NVARCHAR(16) NULL;

ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ADD [OriginalActorRole] NVARCHAR(3) NULL;
GO


UPDATE [dbo].[AggregatedMeasureDataProcesses]
    SET [OriginalActorNumber] = [RequestedByActorNumber]
    WHERE [OriginalActorNumber] IS NULL

UPDATE [dbo].[AggregatedMeasureDataProcesses]
    SET [OriginalActorRole] = [RequestedByActorRole]
    WHERE [OriginalActorRole] IS NULL
GO


ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ALTER COLUMN [OriginalActorNumber] NVARCHAR(16) NOT NULL;

ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ALTER COLUMN [OriginalActorRole] NVARCHAR(3) NOT NULL;
GO