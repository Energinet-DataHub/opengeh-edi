CREATE TABLE b2b.InboxMessages
(
    Id nvarchar(50) NOT NULL,
    RecordId Int IDENTITY (1,1) NOT NULL,
    CONSTRAINT PK_Inbox PRIMARY KEY NONCLUSTERED (Id)
)