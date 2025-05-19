-- Should match the query in OutgoingMessageRepository.GetMessagesReadyToBeBundledQuery()
-- AssignedBundleId does not need to be included as a column since the index is filtered on AssignedBundleId IS NULL
CREATE INDEX IX_OutgoingMessages_MessagesReadyToBeBundled
    ON [dbo].[OutgoingMessages] (
        CreatedAt)
    WHERE AssignedBundleId IS NULL
    WITH (ONLINE = ON)
GO
