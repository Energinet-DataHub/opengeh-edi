ALTER TABLE [dbo].[ArchivedMessages] 
    ADD [EventIds] NVARCHAR(MAX) NULL; -- NVARCHAR(MAX) since an archived message is a bundle of multiple outgoing messages, so we don't know how long the event ids column can be
GO
