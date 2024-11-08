CREATE TABLE [pm].[StepDescription]
(
    [Id]                            UNIQUEIDENTIFIER NOT NULL,
    [RecordId]                      BIGINT IDENTITY (1,1) NOT NULL,

    [Description]                   NVARCHAR(255) NOT NULL,
    [Sequence]                      INT NOT NULL,

    [OrchestrationDescriptionId]    UNIQUEIDENTIFIER NOT NULL,

    -- A UNIQUE CLUSTERED constraint on an INT IDENTITY column optimizes the performance of the table
    -- by ordering indexes by the sequential RecordId column instead of the UNIQUEIDENTIFIER primary key (which is random).
    CONSTRAINT [PK_StepDescription]           PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_StepDescription_RecordId]  UNIQUE CLUSTERED ([RecordId] ASC),

    CONSTRAINT [FK_StepDescription_OrchestrationDescription] FOREIGN KEY ([OrchestrationDescriptionId])
        REFERENCES [pm].[OrchestrationDescription]([Id])
)
GO

CREATE INDEX IX_StepDescription_OrchestrationDescriptionId
    ON [pm].[StepDescription]([OrchestrationDescriptionId]);
GO
