BEGIN TRANSACTION

    -- We need to recreate the index, since the columns has been renamed/changed
    DROP INDEX [UQ_OutgoingMessages_ReceiverNumber_PeriodStartedAt_ReceiverRole_ExternalId] ON [dbo].[OutgoingMessages]
    GO

    -- Index should match the OutgoingMessageRepository.GetIdIfExistsAsync query
    CREATE UNIQUE INDEX UQ_OutgoingMessages_ReceiverNumber_ExternalId_PeriodStartedAt_ReceiverRole ON [dbo].[OutgoingMessages] (
        ReceiverNumber,
        ExternalId,
        PeriodStartedAt,
        ReceiverRole)
            INCLUDE (Id) -- Include Id column since that is what the query selects
    GO

COMMIT TRANSACTION
