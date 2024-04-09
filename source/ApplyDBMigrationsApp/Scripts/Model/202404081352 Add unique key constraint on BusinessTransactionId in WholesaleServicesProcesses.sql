ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ADD CONSTRAINT UC_WholesaleServicesProcesses_BusinessTransactionId_RequestedByActorId UNIQUE (BusinessTransactionId, RequestedByActorId);