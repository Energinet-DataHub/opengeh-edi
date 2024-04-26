CREATE TABLE [dbo].[WholesaleServicesProcessGridAreas] 
(
    [Id]                            UNIQUEIDENTIFIER NOT NULL,
    [RecordId]                      INT IDENTITY (1,1) NOT NULL,

    [WholesaleServicesProcessId]    UNIQUEIDENTIFIER NOT NULL,
    [GridArea]                      NVARCHAR(16) NOT NULL,
    
    CONSTRAINT [PK_WholesaleServicesProcessGridAreas]           PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_WholesaleServicesProcessGridAreas_RecordId]  UNIQUE CLUSTERED ([RecordId] ASC),
)