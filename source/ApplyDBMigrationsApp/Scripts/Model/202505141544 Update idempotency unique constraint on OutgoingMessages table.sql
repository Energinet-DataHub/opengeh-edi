BEGIN TRANSACTION

    DROP INDEX [UQ_OutgoingMessages_ExternalId_ReceiverNumber_ReceiverRole_PeriodStartedAt] ON [dbo].[OutgoingMessages]
    GO

    -- Index should match the OutgoingMessageRepository.GetIdIfExistsAsync query
    CREATE UNIQUE INDEX UQ_OutgoingMessages_ReceiverNumber_PeriodStartedAt_ReceiverRole_ExternalId ON [dbo].[OutgoingMessages] (
        ReceiverNumber,
        PeriodStartedAt,
        ReceiverRole,
        ExternalId)
            INCLUDE (Id) -- Include Id column since that is what the query selects
    GO

COMMIT TRANSACTION
