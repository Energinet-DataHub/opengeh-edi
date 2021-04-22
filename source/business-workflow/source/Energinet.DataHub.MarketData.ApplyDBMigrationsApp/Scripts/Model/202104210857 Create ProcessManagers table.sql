CREATE TABLE [ProcessManagers]
(
    [RecordId]				INT IDENTITY NOT NULL,
    [Id]					UNIQUEIDENTIFIER NOT NULL,
    [ProcessId]				NVARCHAR(36) NOT NULL,
    [EffectiveDate]			DATETIME2(7) NOT NULL,
    [State]			        INT NOT NULL,
    [Type]                  NVARCHAR(200) NOT NULL

    CONSTRAINT [PK_ProcessManagers] PRIMARY KEY ([RecordId]),
    CONSTRAINT [UC_ProcessManagers_Id] UNIQUE ([Id])
)