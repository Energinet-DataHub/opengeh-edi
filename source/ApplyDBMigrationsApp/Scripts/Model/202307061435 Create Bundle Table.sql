CREATE TABLE [dbo].[Bundles]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [Id]                               [uniqueidentifier]   NOT NULL,
    [ActorMessageQueueId]              [uniqueidentifier]   NOT NULL,
    [DocumentTypeInBundle]             [nvarchar](100)      NOT NULL,
    [IsDequeued]                       [bit]                NOT NULL,
    [IsClosed]                         [bit]                NOT NULL,
    [MessageCount]                     [int]                NOT NULL,
    [MaxMessageCount]                  [int]                NOT NULL,
    [BusinessReason]                   [nvarchar](100)      NOT NULL,
    CONSTRAINT [PK_Bundles] PRIMARY KEY NONCLUSTERED ([Id] ASC) ON [PRIMARY],
    CONSTRAINT [FK_ActorMessageQueuesId] FOREIGN KEY ([ActorMessageQueueId]) REFERENCES [dbo].[ActorMessageQueues] ([Id])
)   
ON [PRIMARY];
