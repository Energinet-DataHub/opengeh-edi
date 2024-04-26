CREATE TABLE [dbo].[AggregatedMeasureDataProcessGridAreas] 
(
    [Id]                                UNIQUEIDENTIFIER NOT NULL,
    [RecordId]                          INT IDENTITY (1,1) NOT NULL,

    [AggregatedMeasureDataProcessId]    UNIQUEIDENTIFIER NOT NULL,
    [GridArea]                          NVARCHAR(16) NOT NULL,
    
    CONSTRAINT [PK_AggregatedMeasureDataProcessGridAreas]           PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_AggregatedMeasureDataProcessGridAreas_RecordId]  UNIQUE CLUSTERED ([RecordId] ASC),
    CONSTRAINT [FK_AggregatedMeasureDataProcessGridAreas_AggregatedMeasureDataProcessId] FOREIGN KEY ([AggregatedMeasureDataProcessId]) REFERENCES [dbo].[AggregatedMeasureDataProcesses] ([ProcessId])
)
