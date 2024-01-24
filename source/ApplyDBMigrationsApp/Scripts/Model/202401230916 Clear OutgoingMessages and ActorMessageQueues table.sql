TRUNCATE TABLE [dbo].[OutgoingMessages]
GO

-- We need to delete the bundles first since there is a foreign key referencing ActorMessageQueues
TRUNCATE TABLE [dbo].[Bundles]
GO

-- DELETE FROM since there is a foreign key referencing ActorMessageQueues
-- noinspection SqlWithoutWhere
DELETE FROM [dbo].[ActorMessageQueues]
GO
