BEGIN TRANSACTION

    ALTER TABLE [dbo].[OutgoingMessages]
        ADD [ExternalId_NEW] VARCHAR(36) NULL
    GO

    UPDATE [dbo].[OutgoingMessages]
        SET [ExternalId_NEW] = CONVERT(VARCHAR(36), [ExternalId])

    ALTER TABLE [dbo].[OutgoingMessages]
        ALTER COLUMN [ExternalId_NEW] VARCHAR(36) NOT NULL

COMMIT TRANSACTION
