ALTER TABLE [dbo].[ArchivedMessages] 
    Add [RelatedToMessageId] [nvarchar](36) NULL;

DROP INDEX IX_FindMessages ON [dbo].[ArchivedMessages];

CREATE INDEX [IX_ArchivedMessages_Search_archived_messages] 
    ON [dbo].[ArchivedMessages] 
    ([MessageId], [SenderNumber], [ReceiverNumber], [CreatedAt], [DocumentType], [BusinessReason], [RelatedToMessageId]);