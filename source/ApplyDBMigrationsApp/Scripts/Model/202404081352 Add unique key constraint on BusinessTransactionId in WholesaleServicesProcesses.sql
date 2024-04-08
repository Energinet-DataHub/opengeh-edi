ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ADD CONSTRAINT UC_BusinessTransactionIdAndRequestedByActorId UNIQUE (BusinessTransactionId, RequestedByActorId);