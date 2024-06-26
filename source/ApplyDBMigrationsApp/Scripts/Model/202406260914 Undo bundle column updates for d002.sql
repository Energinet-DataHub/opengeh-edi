-- Undo bundle column updates for d002 - DO NOT COMMIT TO D001
DECLARE @ColumnType NVARCHAR(50);

SELECT @ColumnType = [DATA_TYPE] FROM [INFORMATION_SCHEMA].[COLUMNS] WHERE TABLE_NAME = 'Bundles' AND COLUMN_NAME = 'IsDequeued'

IF @ColumnType = 'datetime2'
BEGIN
    ALTER TABLE [dbo].[Bundles] DROP COLUMN IsDequeued;
    ALTER TABLE [dbo].[Bundles] DROP COLUMN IsClosed;
END

IF @ColumnType = 'datetime2'
BEGIN
    ALTER TABLE [dbo].[Bundles] ADD IsDequeued BIT NOT NULL DEFAULT 0;
    ALTER TABLE [dbo].[Bundles] ADD IsClosed BIT NOT NULL DEFAULT 0;
END
