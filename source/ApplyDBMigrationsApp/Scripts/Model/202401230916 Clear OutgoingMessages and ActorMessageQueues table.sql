TRUNCATE TABLE [dbo].[OutgoingMessages]
GO

-- DELETE FROM since there is a foreign key referencing ActorMessageQueues
-- noinspection SqlWithoutWhere
DELETE FROM [dbo].[ActorMessageQueues]
GO
