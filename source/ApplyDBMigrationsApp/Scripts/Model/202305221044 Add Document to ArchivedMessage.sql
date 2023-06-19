IF NOT EXISTS (
    SELECT *
    FROM   sys.columns
    WHERE  object_id = OBJECT_ID(N'[dbo].[ArchivedMessages]')
      AND name = 'Document'
)
    BEGIN
        ALTER TABLE [dbo].[ArchivedMessages] ADD [Document] [varbinary](max) NULL
    END