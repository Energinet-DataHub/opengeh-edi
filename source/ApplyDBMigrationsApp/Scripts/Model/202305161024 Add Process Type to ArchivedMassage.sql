IF NOT EXISTS (
    SELECT *
    FROM   sys.columns
    WHERE  object_id = OBJECT_ID(N'[dbo].[ArchivedMessages]')
      AND name = 'ProcessType'
)
    BEGIN
        /* is nullable to ensure backward compatibility */
        ALTER TABLE [dbo].[ArchivedMessages]
            ADD [ProcessType] NVARCHAR(50) NULL
    END