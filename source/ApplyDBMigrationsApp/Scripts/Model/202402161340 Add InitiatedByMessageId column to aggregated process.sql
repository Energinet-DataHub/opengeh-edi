ALTER TABLE [dbo].[AggregatedMeasureDataProcesses] 
    Add [InitiatedByMessageId] [nvarchar](36) NULL;

go

UPDATE [dbo].[AggregatedMeasureDataProcesses]
SET [InitiatedByMessageId] = '111111111111111111111111111111111111'
WHERE [InitiatedByMessageId] IS NULL;

go

ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
ALTER COLUMN [InitiatedByMessageId] [nvarchar](36) NOT NULL;

