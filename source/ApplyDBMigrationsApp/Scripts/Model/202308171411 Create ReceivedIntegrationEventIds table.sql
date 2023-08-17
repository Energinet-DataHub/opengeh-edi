CREATE TABLE dbo.ReceivedIntegrationEventIds
(
    Id nvarchar(50) NOT NULL,
    OccurredOn datetime2(7) NOT NULL,
    CONSTRAINT PK_ReceivedIntegrationEventIds PRIMARY KEY NONCLUSTERED (Id)
)
GO