BEGIN TRANSACTION

EXEC sp_rename 'dbo.OutgoingMessages.ExternalId', ExternalId_OLD, 'COLUMN'
GO

EXEC sp_rename 'dbo.OutgoingMessages.ExternalIdBinary', ExternalId, 'COLUMN'
GO

COMMIT TRANSACTION
