BEGIN TRANSACTION

    ALTER TABLE [dbo].[OutgoingMessages]
        ADD [ExternalId_NEW] VARCHAR(36) NULL
    GO

    UPDATE [dbo].[OutgoingMessages]
        SET [ExternalId_NEW] = LOWER(CONVERT(VARCHAR(36), [ExternalId])) -- We must use LOWER() to match Guid.ToString() C# method.

    ALTER TABLE [dbo].[OutgoingMessages]
        ALTER COLUMN [ExternalId_NEW] VARCHAR(36) NOT NULL

COMMIT TRANSACTION
