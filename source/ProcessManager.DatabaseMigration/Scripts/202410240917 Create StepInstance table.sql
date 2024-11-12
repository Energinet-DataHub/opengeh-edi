CREATE TABLE [pm].[StepInstance]
(
    [Id]                            UNIQUEIDENTIFIER NOT NULL,
    [RecordId]                      BIGINT IDENTITY (1,1) NOT NULL,

    [Lifecycle_State]               INT NOT NULL,
    [Lifecycle_TerminationState]    INT NULL,
    [Lifecycle_StartedAt]           DATETIME2 NULL,
    [Lifecycle_TerminatedAt]        DATETIME2 NULL,
    [Lifecycle_CanBeSkipped]        BIT NOT NULL,

    [Description]                   NVARCHAR(255) NOT NULL,
    [Sequence]                      INT NOT NULL,

    [CustomState]                   NVARCHAR(MAX) NOT NULL,
    [OrchestrationInstanceId]       UNIQUEIDENTIFIER NOT NULL,

    -- A UNIQUE CLUSTERED constraint on an INT IDENTITY column optimizes the performance of the table
    -- by ordering indexes by the sequential RecordId column instead of the UNIQUEIDENTIFIER primary key (which is random).
    CONSTRAINT [PK_StepInstance]           PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_StepInstance_RecordId]  UNIQUE CLUSTERED ([RecordId] ASC),

    CONSTRAINT [FK_StepInstance_OrchestrationInstance] FOREIGN KEY ([OrchestrationInstanceId])
        REFERENCES [pm].[OrchestrationInstance]([Id]),
)
GO

CREATE INDEX IX_StepInstance_OrchestrationInstanceId
    ON [pm].[StepInstance]([OrchestrationInstanceId]);
GO
