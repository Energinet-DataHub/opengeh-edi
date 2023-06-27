IF NOT EXISTS (
    SELECT *
    FROM   sys.columns
    WHERE  object_id = OBJECT_ID(N'[dbo].[ArchivedMessages]')
      AND name = 'BusinessReason'
)
    BEGIN
        EXEC sp_rename '[dbo].[ArchivedMessages].[ProcessType]','BusinessReason', 'COLUMN'
    END

IF NOT EXISTS (
    SELECT *
    FROM   sys.columns
    WHERE  object_id = OBJECT_ID(N'[dbo].[OutgoingMessages]')
      AND name = 'BusinessReason'
)
    BEGIN
        EXEC sp_rename '[dbo].[OutgoingMessages].[ProcessType]','BusinessReason', 'COLUMN'
    END

IF NOT EXISTS (
    SELECT *
    FROM   sys.columns
    WHERE  object_id = OBJECT_ID(N'[dbo].[EnqueuedMessages]')
      AND name = 'BusinessReason'
)
    BEGIN
        EXEC sp_rename '[dbo].[EnqueuedMessages].[ProcessType]','BusinessReason', 'COLUMN'
    END