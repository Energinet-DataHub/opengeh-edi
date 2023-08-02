ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    DROP CONSTRAINT PK_AggregatedMeasureDataProcesses;

ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ALTER COLUMN ProcessId [uniqueidentifier] NOT NULL

ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ADD CONSTRAINT PK_AggregatedMeasureDataProcesses PRIMARY KEY (ProcessId);