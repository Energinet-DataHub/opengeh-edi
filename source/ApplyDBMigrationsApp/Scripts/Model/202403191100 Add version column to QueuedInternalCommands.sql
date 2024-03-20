
ALTER TABLE [dbo].[QueuedInternalCommands]
    ADD [CommandVersion] [int] NULL;

go

UPDATE [dbo].[QueuedInternalCommands]
SET [CommandVersion] = 0
WHERE [CommandVersion] IS NULL;

go

ALTER TABLE [dbo].[QueuedInternalCommands]
ALTER COLUMN [CommandVersion] [int] NOT NULL;