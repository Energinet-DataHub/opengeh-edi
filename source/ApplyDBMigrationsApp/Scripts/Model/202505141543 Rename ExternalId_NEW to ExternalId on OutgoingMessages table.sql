BEGIN TRANSACTION

    EXEC sp_rename 'dbo.OutgoingMessages.ExternalId', ExternalId_OLD, 'COLUMN'
    GO

    EXEC sp_rename 'dbo.OutgoingMessages.ExternalId_NEW', ExternalId, 'COLUMN'
    GO

COMMIT TRANSACTION
