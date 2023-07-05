CREATE TABLE [dbo].[Bundles]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [Id]                               [uniqueidentifier]   NOT NULL,
    [ActorMessageQueueId]              [uniqueidentifier]   NOT NULL,
    [DocumentTypeInBundle]             [nvarchar](100)      NOT NULL,
    [IsDequeued]                       [bit]                NOT NULL,
    [IsClosed]                         [bit]                NOT NULL,
    CONSTRAINT [PK_Bundles] PRIMARY KEY NONCLUSTERED ([Id] ASC) ON [PRIMARY],
    CONSTRAINT [FK_ActorMessageQueueId] FOREIGN KEY ([ActorMessageQueueId]) REFERENCES [dbo].[ActorMessageQueue] ([Id])
)   
ON [PRIMARY];
