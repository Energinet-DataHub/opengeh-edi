-- Add a new datetime columns for IsClosed and IsDequeued
ALTER TABLE [dbo].[Bundles] add IsDequeuedTemp datetime2(7) null;
ALTER TABLE [dbo].[Bundles] add IsClosedTemp datetime2(7) null;
GO
-- Update the new datetime columns
UPDATE [dbo].[Bundles] SET IsDequeuedTemp = GETUTCDATE() WHERE IsDequeued = 1;
UPDATE [dbo].[Bundles] SET IsClosedTemp = GETUTCDATE() WHERE IsClosed = 1;
GO
-- Drop the old boolean column
ALTER TABLE [dbo].[Bundles] DROP COLUMN IsDequeued;
ALTER TABLE [dbo].[Bundles] DROP COLUMN IsClosed;
GO
-- Rename the new datetime column (optional)
EXEC sp_rename '[dbo].[Bundles].[IsDequeuedTemp]', 'IsDequeued', 'COLUMN';
EXEC sp_rename '[dbo].[Bundles].[IsClosedTemp]', 'IsClosed', 'COLUMN';
GO