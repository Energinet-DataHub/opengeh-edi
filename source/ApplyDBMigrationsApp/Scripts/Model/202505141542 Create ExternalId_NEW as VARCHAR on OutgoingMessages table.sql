BEGIN TRANSACTION

--     EXEC sp_rename 'dbo.OutgoingMessages.ExternalId', ExternalId_OLD, 'COLUMN'
--     GO

    ALTER TABLE [dbo].[OutgoingMessages]
        ADD [ExternalId_NEW] VARCHAR(36) NULL
    GO

    UPDATE [dbo].[OutgoingMessages]
        SET [ExternalId_NEW] = CONVERT(VARCHAR(36), [ExternalId])

    ALTER TABLE [dbo].[OutgoingMessages]
        ALTER COLUMN [ExternalId_NEW] VARCHAR(36) NOT NULL

COMMIT TRANSACTION
