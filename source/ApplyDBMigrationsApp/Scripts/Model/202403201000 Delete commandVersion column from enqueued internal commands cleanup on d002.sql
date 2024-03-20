ALTER TABLE [dbo].[QueuedInternalCommands]
DROP COLUMN CommandVersion

DELETE FROM [dbo].[SchemaVersions]
WHERE ScriptName = 'Energinet.DataHub.EDI.ApplyDBMigrationsApp.Scripts.Model.202403191100 Add version column to QueuedInternalCommands.sql'