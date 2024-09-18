ALTER TABLE [dbo].[ArchivedMessages]
    ADD [ReceiverRoleCode] NVARCHAR(3) NULL;
ALTER TABLE [dbo].[ArchivedMessages]
    ADD [SenderRoleCode] NVARCHAR(3) NULL;
GO