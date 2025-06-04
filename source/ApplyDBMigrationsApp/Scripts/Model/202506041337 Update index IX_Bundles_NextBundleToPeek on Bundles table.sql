-- Should match the query in BundlesRepository.GetNextBundleToPeekAsync()

-- Drop existing index
DROP INDEX IX_Bundles_NextBundleToPeek
GO

CREATE INDEX IX_Bundles_NextBundleToPeek
    ON [dbo].[Bundles] (
        ActorMessageQueueId,
        DequeuedAt,
        ClosedAt,
        MessageCategory,
        Created) -- Created is last in the index because it is only used to sort the query
    WHERE DequeuedAt IS NULL;
GO

-- ebIX index doesn't include the message category, since ebIX queries messages for all categories
CREATE INDEX IX_Bundles_NextBundleToPeek_ebIX
    ON [dbo].[Bundles] (
        ActorMessageQueueId,
        DequeuedAt,
        ClosedAt,
        Created) -- Created is last in the index because it is only used to sort the query
    WHERE DequeuedAt IS NULL;
GO
