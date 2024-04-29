EXEC sp_rename 'dbo.AggregatedMeasureDataProcesses.RequestedByActorId', RequestedByActorNumber, 'COLUMN'
EXEC sp_rename 'dbo.AggregatedMeasureDataProcesses.RequestedByActorRoleCode', RequestedByActorRole, 'COLUMN'
GO
