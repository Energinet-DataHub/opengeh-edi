CREATE TABLE b2b.ReceivedIntegrationEvents
(
    Id nvarchar(50) NOT NULL,
    RecordId Int IDENTITY (1,1) NOT NULL,
    ProcessedDate datetime2(7) NULL,
    OccurredOn datetime2(7) NOT NULL,
    EventType nvarchar(100) NOT NULL,
    EventPayload varbinary(MAX) NOT NULL,
    ErrorMessage nvarchar(MAX) NULL,
    CONSTRAINT PK_Inbox PRIMARY KEY NONCLUSTERED (Id)
)