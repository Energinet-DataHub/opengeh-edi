DELETE
FROM [dbo].SchemaVersions
WHERE ScriptName like 'Energinet.DataHub.MarketRoles.ApplyDBMigrationsApp.Scripts.Seed%';
