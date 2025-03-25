ALTER TABLE [dbo].[Bundles]
    -- ROWVERSION makes Entity Framework throw an exception if trying to update a row which has already been updated (concurrency conflict)
    -- https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=fluent-api
    ADD [RowVersion] ROWVERSION NOT NULL
GO

CREATE UNIQUE INDEX UQ_Bundles_ActorMessageQueueId_DocumentTypeInBundle
    ON [dbo].Bundles ([ActorMessageQueueId], [DocumentTypeInBundle])
    WHERE [ClosedAt] IS NULL;

GO;
