CREATE TABLE [pm].[OrchestrationStep]
(
    [Id]                            UNIQUEIDENTIFIER NOT NULL,
    [RecordId]                      BIGINT IDENTITY (1,1) NOT NULL,

    [Lifecycle_State]               INT NOT NULL,
    [Lifecycle_TerminationState]    INT NULL,
    [Lifecycle_CreatedAt]           DATETIME2 NOT NULL,
    [Lifecycle_StartedAt]           DATETIME2 NULL,
    [Lifecycle_TerminatedAt]        DATETIME2 NULL,

    [Description]                   NVARCHAR(1000) NULL,

    [Sequence]                      INT NOT NULL,
    [DependsOn]                     UNIQUEIDENTIFIER NULL,

    [CustomState]                   NVARCHAR(255) NOT NULL,
    [OrchestrationInstanceId]       UNIQUEIDENTIFIER NOT NULL,

    -- A UNIQUE CLUSTERED constraint on an INT IDENTITY column optimizes the performance of the table
    -- by ordering indexes by the sequential RecordId column instead of the UNIQUEIDENTIFIER primary key (which is random).
    CONSTRAINT [PK_OrchestrationStep]           PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_OrchestrationStep_RecordId]  UNIQUE CLUSTERED ([RecordId] ASC),

    CONSTRAINT [FK_OrchestrationStep_OrchestrationInstance] FOREIGN KEY ([OrchestrationInstanceId])
        REFERENCES [pm].[OrchestrationInstance]([Id]),

    CONSTRAINT [FK_OrchestrationStep_DependsOn_OrchestrationStep] FOREIGN KEY ([DependsOn])
        REFERENCES [pm].[OrchestrationStep]([Id])
)
GO

CREATE INDEX IX_OrchestrationStep_OrchestrationInstanceId
    ON [pm].[OrchestrationStep]([OrchestrationInstanceId]);
GO
