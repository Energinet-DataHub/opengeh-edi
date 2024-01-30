-- DELETE FROM since Bundles foreign key is referencing ActorMessageQueues
-- noinspection SqlWithoutWhere
DELETE FROM [dbo].[ActorMessageQueues]
GO

TRUNCATE TABLE [dbo].[Bundles]
GO

TRUNCATE TABLE [dbo].[MarketDocuments]
GO

-- MarketDocuments table is cleared for data right before this, to make sure we can add a new NOT NULL column
-- noinspection SqlAddNotNullColumn
ALTER TABLE [dbo].[MarketDocuments] ADD [FileStorageReference] NVARCHAR(1000) NOT NULL
ALTER TABLE [dbo].[MarketDocuments] DROP COLUMN [Payload]
GO
