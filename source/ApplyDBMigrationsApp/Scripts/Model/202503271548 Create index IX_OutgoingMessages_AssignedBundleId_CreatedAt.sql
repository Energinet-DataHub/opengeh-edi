-- Should match the query in OutgoingMessageRepository.GetBundleMetadataForMessagesReadyToBeBundledAsync() (BundleMetadataDto)
CREATE INDEX IX_OutgoingMessages_BundleMetadataForMessagesReadyToBeBundled
    ON [dbo].[OutgoingMessages] (
        AssignedBundleId,
        CreatedAt)
    INCLUDE (
        ReceiverNumber,
        ReceiverRole,
        BusinessReason,
        DocumentType)
    WHERE AssignedBundleId IS NULL
GO
