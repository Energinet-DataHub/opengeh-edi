ALTER TABLE [dbo].[OutgoingMessages]
    ADD [DataCount] INT NULL
GO

UPDATE [dbo].[OutgoingMessages]
    SET [DataCount] = 1
    WHERE [DataCount] IS NULL
GO

ALTER TABLE [dbo].[OutgoingMessages]
    ALTER COLUMN [DataCount] INT NOT NULL;
GO
