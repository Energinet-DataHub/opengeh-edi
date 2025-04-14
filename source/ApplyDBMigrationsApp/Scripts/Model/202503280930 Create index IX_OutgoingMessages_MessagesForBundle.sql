-- Should match the query in OutgoingMessageRepository.GetMessagesForBundleAsync()
-- AssignedBundleId does not need to be included as a column since the index is filtered on AssignedBundleId IS NULL
CREATE INDEX IX_OutgoingMessages_MessagesForBundle
    ON [dbo].[OutgoingMessages] (
        ReceiverNumber,
        ReceiverRole,
        DocumentType,
        BusinessReason,
        RelatedToMessageId)
    WHERE AssignedBundleId IS NULL
GO
