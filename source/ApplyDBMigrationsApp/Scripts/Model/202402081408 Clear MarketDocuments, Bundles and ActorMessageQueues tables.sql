TRUNCATE TABLE [dbo].[Bundles]
GO

-- DELETE FROM since Bundles foreign key is referencing ActorMessageQueues
-- noinspection SqlWithoutWhere
DELETE FROM [dbo].[ActorMessageQueues]
GO

TRUNCATE TABLE [dbo].[MarketDocuments]
GO