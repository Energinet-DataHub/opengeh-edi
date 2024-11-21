UPDATE [pm].[OrchestrationInstance]
   SET [Lifecycle_CreatedBy_IdentityType] = 'NULL-VALUE-SET-BY-SCRIPT' -- <-- Use a value that makes it easy to determine the value is set by this script in the future
   WHERE [Lifecycle_CreatedBy_IdentityType] IS NULL
GO

ALTER TABLE [pm].[OrchestrationInstance]
    ALTER COLUMN [Lifecycle_CreatedBy_IdentityType] NVARCHAR(255) NOT NULL
GO