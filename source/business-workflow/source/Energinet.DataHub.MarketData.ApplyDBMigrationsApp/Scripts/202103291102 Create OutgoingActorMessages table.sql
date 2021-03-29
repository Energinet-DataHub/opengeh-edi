CREATE TABLE dbo.OutgoingActorMessages
(
    Id            int IDENTITY,
    OccurredOn     datetime2(2) NOT NULL,
    Type          nchar(50)    NOT NULL,
    Data          text         NOT NULL,
    LastUpdatedOn datetime2(2),
    State         int          NOT NULL
)
GO
