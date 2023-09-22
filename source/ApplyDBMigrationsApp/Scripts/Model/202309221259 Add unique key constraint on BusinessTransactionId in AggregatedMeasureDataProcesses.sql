ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ADD CONSTRAINT UC_BusinessTransactionId UNIQUE (BusinessTransactionId);
