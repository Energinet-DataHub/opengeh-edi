TRUNCATE TABLE [dbo].[ArchivedMessages]
GO

-- ArchivedMessages table is cleared for data right before this, to make sure we can add a new NOT NULL column
-- noinspection SqlAddNotNullColumn
ALTER TABLE [dbo].[ArchivedMessages] ADD [FileStorageReference] NVARCHAR(1000) NOT NULL
ALTER TABLE [dbo].[ArchivedMessages] DROP COLUMN [Document]
GO