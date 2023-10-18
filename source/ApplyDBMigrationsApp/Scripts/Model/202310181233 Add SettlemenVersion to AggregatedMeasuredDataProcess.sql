IF NOT EXISTS (
    SELECT *
    FROM   sys.columns
    WHERE  object_id = OBJECT_ID(N'[dbo].[AggregatedMeasureDataProcesses]')
      AND name = 'SettlementVersion'
)
BEGIN
ALTER TABLE [dbo].[AggregatedMeasureDataProcesses] ADD [SettlementVersion] [varbinary](3) NULL
END