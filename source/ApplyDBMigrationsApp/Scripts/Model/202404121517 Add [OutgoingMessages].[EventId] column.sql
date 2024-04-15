ALTER TABLE [dbo].[OutgoingMessages] 
    ADD [EventId] NVARCHAR(100) NULL;
GO

UPDATE [dbo].[OutgoingMessages]
    SET [EventId] = 'UNKNOWN-BECAUSE-OLD'
    WHERE [EventId] IS NULL
GO

ALTER TABLE [dbo].[OutgoingMessages]
    ALTER COLUMN [EventId] NVARCHAR(100) NOT NULL;
GO