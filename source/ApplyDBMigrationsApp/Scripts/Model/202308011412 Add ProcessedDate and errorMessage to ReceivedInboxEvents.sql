DROP TABLE [dbo].[ReceivedInboxEvents];
GO

CREATE TABLE [dbo].[ReceivedInboxEvents]
(
    Id nvarchar(50)                 NOT NULL,
    RecordId Int IDENTITY (1,1)     NOT NULL,
    OccurredOn datetime2(7)         NOT NULL,
    EventType nvarchar(100)         NOT NULL,
    EventPayload nvarchar(MAX)      NOT NULL,
    ReferenceId [uniqueidentifier]  NOT NULL,
    ProcessedDate datetime2(7)      NULL,
    ErrorMessage nvarchar(MAX)      NULL,
    CONSTRAINT PK_ReceivedInboxEvents PRIMARY KEY NONCLUSTERED (Id)
)
