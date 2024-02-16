ALTER TABLE [dbo].[AggregatedMeasureDataProcesses] 
    Add [InitiatedByMessageId] [nvarchar](36) NULL;

go

UPDATE [dbo].[AggregatedMeasureDataProcesses]
SET [InitiatedByMessageId] = NEWID()
WHERE [InitiatedByMessageId] IS NULL;

go

ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
ALTER COLUMN [InitiatedByMessageId] [nvarchar](36) NOT NULL;

