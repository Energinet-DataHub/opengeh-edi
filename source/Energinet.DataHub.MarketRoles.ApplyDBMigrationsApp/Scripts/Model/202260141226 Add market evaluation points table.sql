CREATE TABLE [b2b].[MarketEvaluationPoints]
(
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [MarketEvaluationPointNumber] [nvarchar](50) NOT NULL UNIQUE,
    [EnergySupplierNumber] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_MarketEvaluationPoints] PRIMARY KEY NONCLUSTERED ([Id])
)
