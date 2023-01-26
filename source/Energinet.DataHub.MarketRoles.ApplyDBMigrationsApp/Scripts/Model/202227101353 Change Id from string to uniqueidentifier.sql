ALTER TABLE [b2b].[AggregatedTimeSeriesTransactions]
DROP CONSTRAINT DF_Id
ALTER TABLE [b2b].[AggregatedTimeSeriesTransactions]
DROP CONSTRAINT PK_AggregatedTimeSeriesTransactions
ALTER TABLE [b2b].[AggregatedTimeSeriesTransactions]
ALTER COLUMN Id uniqueidentifier NOT NULL
ALTER TABLE [b2b].[AggregatedTimeSeriesTransactions]
    ADD CONSTRAINT PK_AggregatedTimeSeriesTransactions PRIMARY KEY (Id)

ALTER TABLE [b2b].[MoveInTransactions]
DROP CONSTRAINT PK_Transactions
ALTER TABLE [b2b].[MoveInTransactions]
ALTER COLUMN TransactionId uniqueidentifier NOT NULL
ALTER TABLE [b2b].[MoveInTransactions]
    ADD CONSTRAINT PK_MoveInTransactions PRIMARY KEY (TransactionId)

DECLARE @ConstraintName nvarchar(200)
SELECT @ConstraintName = Name FROM SYS.DEFAULT_CONSTRAINTS WHERE PARENT_OBJECT_ID = OBJECT_ID('b2b.OutgoingMessages') AND PARENT_COLUMN_ID = (SELECT column_id FROM sys.columns WHERE NAME = N'TransactionId' AND object_id = OBJECT_ID(N'b2b.OutgoingMessages'))
    IF @ConstraintName IS NOT NULL
EXEC('ALTER TABLE b2b.OutgoingMessages DROP CONSTRAINT ' + @ConstraintName)
ALTER TABLE [b2b].[OutgoingMessages]
ALTER COLUMN TransactionId uniqueidentifier NOT NULL