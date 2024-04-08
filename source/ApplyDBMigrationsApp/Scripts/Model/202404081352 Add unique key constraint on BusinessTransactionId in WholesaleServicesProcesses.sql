ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ADD CONSTRAINT UC_WholeBusinessTransactionIdAndRequestedByActorId UNIQUE (BusinessTransactionId, RequestedByActorId);