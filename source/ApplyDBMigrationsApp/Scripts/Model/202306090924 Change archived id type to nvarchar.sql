ALTER TABLE [dbo].[ArchivedMessages]
    DROP CONSTRAINT PK_ArchivedMessages_Id;

DROP INDEX IX_FindMessages ON [dbo].[ArchivedMessages];

ALTER TABLE [dbo].[ArchivedMessages]
    ALTER COLUMN Id [nvarchar](50) NOT NULL

alter table [dbo].[ArchivedMessages]
    ADD CONSTRAINT PK_ArchivedMessages_Id PRIMARY KEY (id);

CREATE INDEX IX_FindMessages ON [dbo].[ArchivedMessages] (Id, DocumentType, ReceiverNumber, SenderNumber, CreatedAt);

ALTER TABLE [dbo].[ArchivedMessages]
    ADD [Document] [varbinary](max) NULL 