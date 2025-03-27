CREATE UNIQUE INDEX UQ_Bundles_ActorMessageQueueId_DocumentTypeInBundle_BusinessReason_RelatedToMessageId
    ON [dbo].Bundles (
        [ActorMessageQueueId],
        [DocumentTypeInBundle],
        [BusinessReason],
        [RelatedToMessageId])
    WHERE [ClosedAt] IS NULL;
GO
