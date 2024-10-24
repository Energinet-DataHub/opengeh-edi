CREATE TABLE [pm].[OrchestrationDescription]
(
    [Id]                UNIQUEIDENTIFIER NOT NULL,
    [RecordId]          BIGINT IDENTITY (1,1) NOT NULL,

    [Name]                  NVARCHAR(255) NOT NULL,
    [Version]               INT NOT NULL,
    [CanBeScheduled]        BIT NOT NULL,
    [HostName]              NVARCHAR(255) NOT NULL,
    [IsEnabled]             BIT NOT NULL,
    [FunctionName]          NVARCHAR(255) NOT NULL,
    [ParameterDefinition]   NVARCHAR(MAX) NOT NULL,

    -- A UNIQUE CLUSTERED constraint on an INT IDENTITY column optimizes the performance of the table
    -- by ordering indexes by the sequential RecordId column instead of the UNIQUEIDENTIFIER primary key (which is random).
    CONSTRAINT [PK_OrchestrationDescription]            PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_OrchestrationDescription_RecordId]   UNIQUE CLUSTERED ([RecordId] ASC),
)
GO
