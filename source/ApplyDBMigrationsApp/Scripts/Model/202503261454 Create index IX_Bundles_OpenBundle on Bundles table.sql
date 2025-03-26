CREATE INDEX IX_Bundles_OpenBundle
    ON [dbo].[Bundles] (
        ActorMessageQueueId,
        DocumentTypeInBundle,
        BusinessReason,
        RelatedToMessageId,
        ClosedAt,
        Created) -- Created is last in the index because the sql query orders by Created (doesn't filter by it).
    WHERE ClosedAt IS NULL;
GO
