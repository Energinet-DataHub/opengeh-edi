ALTER TABLE [dbo].[ActorMessageQueues] ADD CONSTRAINT [Unique_ActorRole_ActorNumber] UNIQUE (ActorRole, ActorNumber);
GO