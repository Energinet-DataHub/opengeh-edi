-- Should match the query in OutgoingMessageRepository.GetBundleMetadataForMessagesReadyToBeBundledAsync() (BundleMetadataDto)
-- AssignedBundleId does not need to be included as a column since the index is filtered on AssignedBundleId IS NULL
DROP INDEX IX_OutgoingMessages_BundleMetadataForMessagesReadyToBeBundled ON [dbo].[OutgoingMessages]
GO

CREATE INDEX IX_OutgoingMessages_BundleMetadataForMessagesReadyToBeBundled
    ON [dbo].[OutgoingMessages] (
        CreatedAt,
        ReceiverNumber,
        ReceiverRole,
        DocumentType,
        BusinessReason,
        RelatedToMessageId)
    WHERE AssignedBundleId IS NULL
GO
