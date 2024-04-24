ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ADD [RequestedForActorNumber] NVARCHAR(16) NULL;

ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ADD [RequestedForActorRole] NVARCHAR(3) NULL;
GO


UPDATE [dbo].[WholesaleServicesProcesses]
    SET [RequestedForActorNumber] = [RequestedByActorNumber]
    WHERE [RequestedForActorNumber] IS NULL

UPDATE [dbo].[WholesaleServicesProcesses]
    SET [RequestedForActorRole] = [RequestedByActorRole]
    WHERE [RequestedForActorRole] IS NULL
GO


ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ALTER COLUMN [RequestedForActorNumber] NVARCHAR(16) NOT NULL;

ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ALTER COLUMN [RequestedForActorRole] NVARCHAR(3) NOT NULL;
GO