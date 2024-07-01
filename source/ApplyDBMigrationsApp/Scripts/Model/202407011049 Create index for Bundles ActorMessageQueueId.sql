-- Index used when retrieving Bundles for in a specific ActorMessageQueue

CREATE NONCLUSTERED INDEX [IX_Bundles_ActorMessageQueueId]
    ON [dbo].[Bundles] ([ActorMessageQueueId])
    INCLUDE (
             [BusinessReason],
             [ClosedAt],
             [Created],
             [DequeuedAt],
             [DocumentTypeInBundle],
             [Id],
             [MaxMessageCount],
             [MessageCount],
             [MessageId],
             [PeekedAt],
             [RelatedToMessageId])
