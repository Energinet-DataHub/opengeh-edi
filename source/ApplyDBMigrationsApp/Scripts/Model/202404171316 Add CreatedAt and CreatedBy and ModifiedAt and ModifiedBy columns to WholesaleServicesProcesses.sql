ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ADD [CreatedBy] NVARCHAR(100) NULL,
    [CreatedAt] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [ModifiedAt] DATETIME2 NULL;
GO

UPDATE [dbo].[WholesaleServicesProcesses]
SET [CreatedBy] = 'UNKNOWN-BECAUSE-OLD',
    [CreatedAt] = SYSUTCDATETIME()
WHERE [CreatedBy] IS NULL
GO

ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ALTER COLUMN [CreatedBy] NVARCHAR(100) NOT NULL;
ALTER TABLE [dbo].[WholesaleServicesProcesses]
    ALTER COLUMN [CreatedAt] DATETIME2 NOT NULL;
GO