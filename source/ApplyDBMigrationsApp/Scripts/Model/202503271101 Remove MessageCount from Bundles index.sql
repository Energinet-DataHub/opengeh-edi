-- Index used when retrieving Bundles for in a specific ActorMessageQueue
DROP INDEX IX_Bundles_ActorMessageQueueId ON [dbo].[Bundles]
GO

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
             [MessageId],
             [PeekedAt],
             [RelatedToMessageId])
GO
