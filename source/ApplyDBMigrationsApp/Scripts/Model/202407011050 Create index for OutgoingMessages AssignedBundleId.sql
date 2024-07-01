-- Index used when finding OutgoingMessages in a specific Bundle

CREATE NONCLUSTERED INDEX [IX_OutgoingMessages_AssignedBundleId]
    ON [dbo].[OutgoingMessages] ([AssignedBundleId])
