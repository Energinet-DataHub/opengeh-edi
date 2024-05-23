ALTER TABLE [dbo].[Bundles]
    ADD [MessageId] NVARCHAR(16) NULL;
GO

UPDATE [dbo].[Bundles]
SET [MessageId] = [Id]
WHERE [MessageId] IS NULL;
GO

ALTER TABLE [dbo].[Bundles]
ALTER
COLUMN [MessageId] NVARCHAR(36) NOT NULL;
GO