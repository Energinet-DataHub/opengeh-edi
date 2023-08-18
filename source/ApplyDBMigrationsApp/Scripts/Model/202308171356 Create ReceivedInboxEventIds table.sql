CREATE TABLE dbo.ReceivedInboxEventIds
(
    Id nvarchar(50) NOT NULL,
    OccurredOn datetime2(7) NOT NULL,
    CONSTRAINT PK_ReceivedInboxEventIds PRIMARY KEY NONCLUSTERED (Id)
)
GO 