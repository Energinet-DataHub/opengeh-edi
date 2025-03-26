CREATE INDEX IX_Bundles_OldestBundle
    ON [dbo].[Bundles] (
        ActorMessageQueueId,
        DequeuedAt,
        MessageCategory,
        ClosedAt,
        Created);
GO
