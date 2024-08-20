ALTER TABLE [dbo].[OutgoingMessages]
    ADD [PeriodStartedAt] DATETIME2 NOT NULL DEFAULT dateadd(DD,-14,getdate());
GO