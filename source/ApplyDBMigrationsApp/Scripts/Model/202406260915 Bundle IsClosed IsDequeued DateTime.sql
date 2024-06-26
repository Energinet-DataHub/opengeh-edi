-- Add a new datetime columns for IsClosed and IsDequeued
ALTER TABLE [dbo].[Bundles] add DequeuedAt datetime2(7) null;
ALTER TABLE [dbo].[Bundles] add ClosedAt datetime2(7) null;
GO
-- Update the new datetime columns
UPDATE [dbo].[Bundles] SET DequeuedAt = GETUTCDATE() WHERE IsDequeued = 1;
UPDATE [dbo].[Bundles] SET ClosedAt = GETUTCDATE() WHERE IsClosed = 1;
GO
-- Drop the old boolean column
ALTER TABLE [dbo].[Bundles] DROP COLUMN IsDequeued;
ALTER TABLE [dbo].[Bundles] DROP COLUMN IsClosed;
GO
