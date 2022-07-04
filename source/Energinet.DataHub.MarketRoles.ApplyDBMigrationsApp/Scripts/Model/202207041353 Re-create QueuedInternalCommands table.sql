DROP TABLE [dbo].[QueuedInternalCommands]
    GO
CREATE TABLE [dbo].[QueuedInternalCommands]
(
    [Id]            [uniqueidentifier]   NOT NULL,
    [RecordId]      [int] IDENTITY (1,1) NOT NULL,
    [Type]          [nvarchar](255)      NOT NULL,
    [Data]          [nvarchar](max)      NOT NULL,
    [ProcessedDate] [datetime2](1)       NULL,
    [CreationDate]  [datetime2](7)       NOT NULL,
    [ErrorMessage]  [nvarchar](max)      NULL,
    CONSTRAINT [PK_QueuedInternalCommands] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UC_QueuedInternalCommands_Id] UNIQUE CLUSTERED
(
[RecordId] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]