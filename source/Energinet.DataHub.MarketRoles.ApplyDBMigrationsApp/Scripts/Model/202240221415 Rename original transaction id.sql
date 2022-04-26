ALTER TABLE [b2b].[OutgoingMessages]
    DROP COLUMN [OriginalTransactionId]

ALTER TABLE [b2b].[OutgoingMessages]
    ADD [OriginalMessageId] [nvarchar](50) NOT NULL DEFAULT('None');