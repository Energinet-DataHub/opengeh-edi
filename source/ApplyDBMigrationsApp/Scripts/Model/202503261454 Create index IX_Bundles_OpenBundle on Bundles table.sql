CREATE INDEX IX_Bundles_OpenBundle
    ON [dbo].[Bundles] (
        ActorMessageQueueId,
        DocumentTypeInBundle,
        BusinessReason,
        RelatedToMessageId,
        ClosedAt,
        Created)
    WHERE ClosedAt IS NULL;
GO
