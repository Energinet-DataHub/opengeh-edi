ALTER TABLE [dbo].[Actor]
    DROP CONSTRAINT Unique_IdentificationNumber_B2CId;
     
ALTER TABLE [dbo].[Actor]
    ALTER COLUMN ActorNumber nvarchar(16) NOT NULL;

ALTER TABLE [dbo].[Actor]
    ADD CONSTRAINT [Unique_ActorNumber_ExternalId] UNIQUE (ActorNumber, ExternalId);