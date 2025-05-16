BEGIN TRANSACTION

    ALTER TABLE [dbo].[OutgoingMessages]
        ADD [ExternalIdBinary] BINARY(16) NULL
    GO

    -- Delete all rows that isn't a GUID. This can be done because all rows should be valid guids in production.
    DELETE FROM [dbo].[OutgoingMessages]
        WHERE TRY_CAST([ExternalId] AS UNIQUEIDENTIFIER) IS NULL;
    GO

    UPDATE [dbo].[OutgoingMessages]
        SET [ExternalIdBinary] = CAST(CAST([ExternalId] AS UNIQUEIDENTIFIER) AS BINARY(16));

    ALTER TABLE [dbo].[OutgoingMessages]
        ALTER COLUMN [ExternalIdBinary] BINARY(16) NOT NULL

COMMIT TRANSACTION
