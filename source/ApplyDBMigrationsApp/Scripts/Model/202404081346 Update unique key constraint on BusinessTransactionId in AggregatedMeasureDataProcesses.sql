ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    DROP CONSTRAINT UC_BusinessTransactionId
         
ALTER TABLE [dbo].[AggregatedMeasureDataProcesses]
    ADD CONSTRAINT UC_AggregatedMeasureDataProcesses_BusinessTransactionId_RequestedByActorId UNIQUE (BusinessTransactionId, RequestedByActorId);