-- Should match the query in BundlesRepository.GetNextBundleToPeekAsync()
CREATE INDEX IX_Bundles_NextBundleToPeek
    ON [dbo].[Bundles] (
        ActorMessageQueueId,
        DequeuedAt,
        ClosedAt,
        Created,
        MessageCategory) -- MessageCategory is last in the index because MessageCategory isn't always added to the sql query.
    WHERE DequeuedAt IS NULL;
GO