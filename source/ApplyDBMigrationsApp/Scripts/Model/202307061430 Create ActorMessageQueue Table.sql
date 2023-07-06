CREATE TABLE [dbo].[ActorMessageQueues]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [Id]                               [uniqueidentifier] NOT NULL,
    [ActorRole]                        [nvarchar](50)     NOT NULL,
    [ActorNumber]                      [nvarchar](50)     NOT NULL,
    [BusinessReason]                   [nvarchar](50)     NOT NULL,
    CONSTRAINT [PK_ActorMessageQueues] PRIMARY KEY NONCLUSTERED
([Id] ASC) ON [PRIMARY]
) ON [PRIMARY];