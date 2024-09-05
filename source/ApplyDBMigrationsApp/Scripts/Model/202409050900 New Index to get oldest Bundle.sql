CREATE INDEX IX_OldestBundle ON [dbo].[Bundles] (ActorMessageQueueId, DequeuedAt, MessageCategory);
GO