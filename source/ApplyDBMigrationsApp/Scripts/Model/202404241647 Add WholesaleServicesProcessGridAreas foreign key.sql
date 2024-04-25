ALTER TABLE [dbo].[WholesaleServicesProcessGridAreas] 
    ADD CONSTRAINT [FK_WholesaleServicesProcessGridAreas_WholesaleServicesProcessId]
        FOREIGN KEY ([WholesaleServicesProcessId])
        REFERENCES [dbo].[WholesaleServicesProcesses] ([ProcessId])
