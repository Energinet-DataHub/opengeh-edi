ALTER TABLE [dbo].[OutgoingMessages]
    ADD [PeriodStartedAt] DATETIME2 NULL DEFAULT dateadd(DD,-14,getdate());
GO