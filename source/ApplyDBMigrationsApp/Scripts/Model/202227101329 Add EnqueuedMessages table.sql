CREATE TABLE [B2B].EnqueuedMessages(
    [RecordId]                        [int] IDENTITY (1,1) NOT NULL,
    [Id]                              [uniqueIdentifier] NOT NULL,
    [DocumentType]                    [VARCHAR](255)     NOT NULL,
    [MessageCategory]                 [VARCHAR](255)     NOT NULL,
    [ReceiverId]                      [VARCHAR](255)     NOT NULL,
    [ReceiverRole]                    [VARCHAR](50)      NOT NULL,
    [SenderId]                        [VARCHAR](255)     NOT NULL,
    [SenderRole]                      [VARCHAR](50)      NOT NULL,
    [ProcessType]                     [VARCHAR](50)      NOT NULL,
    [Payload]                         [NVARCHAR](MAX)    NOT NULL,
    CONSTRAINT [PK_EnqueuedMessages_Id] PRIMARY KEY NONCLUSTERED
(
    [Id] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY];

CREATE INDEX IX_FindMessages ON [B2B].[EnqueuedMessages] (ProcessType, ReceiverId, ReceiverRole, DocumentType, MessageCategory);
CREATE INDEX IX_FindOldestMessage ON [B2B].[EnqueuedMessages] (ProcessType, ReceiverId, MessageCategory);