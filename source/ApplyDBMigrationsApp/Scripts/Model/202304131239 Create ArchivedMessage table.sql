CREATE TABLE [B2B].ArchivedMessages(
    [RecordId]                        [int] IDENTITY (1,1) NOT NULL,
    [Id]                              [uniqueIdentifier] NOT NULL,
    [DocumentType]                    [VARCHAR](255)     NOT NULL,
    [ReceiverNumber]                  [VARCHAR](255)     NOT NULL,
    [SenderNumber]                    [VARCHAR](255)     NOT NULL,
    [CreatedAt]                       [datetime2](7)     NOT NULL,
    CONSTRAINT [PK_ArchivedMessages_Id] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY];

CREATE INDEX IX_FindMessages ON [B2B].[EnqueuedMessages] (Id, DocumentType, ReceiverNumber, SenderNumber, CreatedAt);
