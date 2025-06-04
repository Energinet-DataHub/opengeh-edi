-- Should match the query in BundlesRepository.GetNextBundleToPeekAsync()

-- Drop existing index
DROP INDEX IX_Bundles_NextBundleToPeek
GO

-- DequeuedAt does not need to be included in the key columns since the index is filtered on DequeuedAt IS NULL
CREATE INDEX IX_Bundles_NextBundleToPeek
    ON [dbo].[Bundles] (
        ActorMessageQueueId,
        ClosedAt,
        MessageCategory,
        Created) -- Created is last in the index because it is only used to sort the query
    WHERE DequeuedAt IS NULL;
GO

-- ebIX index doesn't include the message category, since the ebIX query is for all categories
CREATE INDEX IX_Bundles_NextBundleToPeek_ebIX
    ON [dbo].[Bundles] (
        ActorMessageQueueId,
        ClosedAt,
        Created) -- Created is last in the index because it is only used to sort the query
    WHERE DequeuedAt IS NULL;
GO
