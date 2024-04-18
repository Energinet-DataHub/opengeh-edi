ALTER TABLE [dbo].[MarketDocuments]
    ADD [CreatedBy] NVARCHAR(100) NULL,
    [CreatedAt] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [ModifiedAt] DATETIME2 NULL;
GO

UPDATE [dbo].[MarketDocuments]
SET [CreatedBy] = 'UNKNOWN-BECAUSE-OLD',
    [CreatedAt] = SYSUTCDATETIME()
WHERE [CreatedBy] IS NULL
GO

ALTER TABLE [dbo].[MarketDocuments]
    ALTER COLUMN [CreatedBy] NVARCHAR(100) NOT NULL;
ALTER TABLE [dbo].[MarketDocuments]
    ALTER COLUMN [CreatedAt] DATETIME2 NOT NULL;
GO