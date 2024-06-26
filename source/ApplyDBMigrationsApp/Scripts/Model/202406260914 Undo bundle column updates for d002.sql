-- Undo bundle column updates for d002 - DO NOT COMMIT TO D001
ALTER TABLE [dbo].[Bundles] DROP COLUMN IsDequeued;
ALTER TABLE [dbo].[Bundles] DROP COLUMN IsClosed;
GO

ALTER TABLE [dbo].[Bundles] ADD IsDequeued BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[Bundles] ADD IsClosed BIT NOT NULL DEFAULT 0;
GO
