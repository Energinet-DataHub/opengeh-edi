ALTER TABLE [dbo].[Actor]
DROP CONSTRAINT Unique_IdentificationNumber_B2CId;

ALTER TABLE [dbo].[Actor]
ALTER COLUMN ExternalId [nvarchar](36) NOT NULL
      
ALTER TABLE [dbo].[Actor] 
    ADD CONSTRAINT [Unique_IdentificationNumber_B2CId] UNIQUE (ActorNumber, ExternalId);