ALTER TABLE [dbo].[ArchivedMessages]
    DROP CONSTRAINT PK_ArchivedMessages_Id;

ALTER TABLE [dbo].[ArchivedMessages] 
    ALTER COLUMN [Id] UNIQUEIDENTIFIER NOT NULL

ALTER TABLE [dbo].[ArchivedMessages]
    ADD CONSTRAINT PK_ArchivedMessages_Id PRIMARY KEY (Id);
