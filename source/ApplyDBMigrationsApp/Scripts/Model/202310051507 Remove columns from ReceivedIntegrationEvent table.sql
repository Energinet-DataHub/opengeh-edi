DROP INDEX IX_ProcessDate ON [dbo].[ReceivedIntegrationEvents];

ALTER TABLE [dbo].[ReceivedIntegrationEvents]
    DROP COLUMN [ProcessedDate], [ErrorMessage];