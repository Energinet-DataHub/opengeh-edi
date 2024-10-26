CREATE TABLE [pm].[OrchestrationInstance]
(
    [Id]                            UNIQUEIDENTIFIER NOT NULL,
    [RecordId]                      BIGINT IDENTITY (1,1) NOT NULL,

    [Lifecycle_State]               INT NOT NULL,
    [Lifecycle_TerminationState]    INT NULL,
    [Lifecycle_CreatedAt]           DATETIME2 NOT NULL,
    [Lifecycle_ScheduledToRunAt]    DATETIME2 NULL,
    [Lifecycle_StartRequestedAt]    DATETIME2 NULL,
    [Lifecycle_StartedAt]           DATETIME2 NULL,
    [Lifecycle_TerminatedAt]        DATETIME2 NULL,

    [ParameterValue]                NVARCHAR(MAX) NOT NULL,
    [CustomState]                   NVARCHAR(255) NOT NULL,
    [OrchestrationDescriptionId]    UNIQUEIDENTIFIER NOT NULL,

    -- A UNIQUE CLUSTERED constraint on an INT IDENTITY column optimizes the performance of the table
    -- by ordering indexes by the sequential RecordId column instead of the UNIQUEIDENTIFIER primary key (which is random).
    CONSTRAINT [PK_OrchestrationInstance]           PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_OrchestrationInstance_RecordId]  UNIQUE CLUSTERED ([RecordId] ASC),

    CONSTRAINT [FK_OrchestrationInstance_OrchestrationDescription] FOREIGN KEY ([OrchestrationDescriptionId])
        REFERENCES [pm].[OrchestrationDescription]([Id])
)
GO

-- TODO: Indexes needs to be created for the OrchestrationInstance table based on how the instances will be queried
