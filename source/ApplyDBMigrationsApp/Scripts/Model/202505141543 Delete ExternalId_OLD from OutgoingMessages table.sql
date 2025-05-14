BEGIN TRANSACTION

    EXEC sp_rename 'dbo.OutgoingMessages.ExternalId', ExternalId_OLD, 'COLUMN'

    ALTER TABLE [dbo].[OutgoingMessages]
        ADD [ExternalId] VARCHAR(36) NULL

    UPDATE [dbo].[OutgoingMessages]
        SET [ExternalId] = CONVERT(VARCHAR(36), ExternalId_OLD)

    ALTER TABLE [dbo].[OutgoingMessages]
        ALTER COLUMN [ExternalId] VARCHAR(36) NOT NULL

COMMIT TRANSACTION
