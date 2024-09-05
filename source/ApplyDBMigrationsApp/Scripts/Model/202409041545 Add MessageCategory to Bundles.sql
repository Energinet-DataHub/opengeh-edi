ALTER TABLE [dbo].[Bundles] ADD [MessageCategory] NVARCHAR(16) NULL
GO

UPDATE [dbo].[Bundles] SET [MessageCategory] = 'Aggregations'
GO

ALTER TABLE [dbo].[Bundles]
ALTER COLUMN [MessageCategory] NVARCHAR(16) NOT NULL;
GO