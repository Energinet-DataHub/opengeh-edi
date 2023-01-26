ALTER TABLE [b2b].[AggregatedTimeSeriesTransactions]
DROP CONSTRAINT PK_AggregatedTimeSeriesTransactions_Id
ALTER TABLE [b2b].[AggregatedTimeSeriesTransactions]
ALTER COLUMN Id nvarchar(50) NOT NULL
ALTER TABLE [b2b].[AggregatedTimeSeriesTransactions]
    ADD CONSTRAINT DF_Id DEFAULT '' FOR Id
ALTER TABLE [b2b].[AggregatedTimeSeriesTransactions]
    ADD CONSTRAINT PK_AggregatedTimeSeriesTransactions PRIMARY KEY (Id)