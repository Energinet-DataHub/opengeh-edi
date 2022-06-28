IF OBJECT_ID(N'b2b.MarketEvaluationPoints', N'U') IS NULL
    BEGIN
        CREATE TABLE [b2b].[MarketEvaluationPoints]
        (
            [Id]                          [uniqueidentifier]   NOT NULL,
            [RecordId]                    [int] IDENTITY (1,1) NOT NULL,
            [MarketEvaluationPointNumber] [nvarchar](50)       NOT NULL,
            [EnergySupplierNumber]        [nvarchar](50)       NOT NULL,
            CONSTRAINT [PK_MarketEvaluationPoints] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
            UNIQUE NONCLUSTERED
                (
                 [MarketEvaluationPointNumber] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
go

IF OBJECT_ID(N'b2b.MessageIds', N'U') IS NULL
    BEGIN
        CREATE TABLE [b2b].[MessageIds]
        (
            [RecordId]  [int] IDENTITY (1,1) NOT NULL,
            [MessageId] [nvarchar](50)       NOT NULL,
            CONSTRAINT [PK_MessageIds] PRIMARY KEY NONCLUSTERED
                (
                 [MessageId] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
go

IF OBJECT_ID(N'b2b.MoveInTransactions', N'U') IS NULL
    BEGIN
        CREATE TABLE [b2b].[MoveInTransactions]
        (
            [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
            [TransactionId]                    [nvarchar](50)       NOT NULL,
            [ProcessId]                        [nvarchar](50)       NULL,
            [MarketEvaluationPointId]          [nvarchar](50)       NULL,
            [EffectiveDate]                    [datetime2](7)       NULL,
            [CurrentEnergySupplierId]          [nvarchar](50)       NULL,
            [State]                            [nvarchar](50)       NOT NULL,
            [StartedByMessageId]               [nvarchar](50)       NOT NULL,
            [NewEnergySupplierId]              [nvarchar](50)       NOT NULL,
            [ConsumerId]                       [nvarchar](50)       NULL,
            [ConsumerName]                     [nvarchar](255)      NULL,
            [ConsumerIdType]                   [nvarchar](50)       NULL,
            [ForwardedMeteringPointMasterData] [bit]                NOT NULL,
            CONSTRAINT [PK_Transactions] PRIMARY KEY NONCLUSTERED
                (
                 [TransactionId] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY];

        ALTER TABLE [b2b].[MoveInTransactions]
            ADD CONSTRAINT [DF_State] DEFAULT ('Started') FOR [State]

        ALTER TABLE [b2b].[MoveInTransactions]
            ADD CONSTRAINT [DF_StartedByMessageId] DEFAULT ('NotSet') FOR [StartedByMessageId]

        ALTER TABLE [b2b].[MoveInTransactions]
            ADD CONSTRAINT [DF_NewEnergySupplierId] DEFAULT ('NotSet') FOR [NewEnergySupplierId]

        ALTER TABLE [b2b].[MoveInTransactions]
            ADD CONSTRAINT [DF_ForwardedMeteringPointMasterData] DEFAULT ((0)) FOR [ForwardedMeteringPointMasterData]
    END
go

IF OBJECT_ID(N'b2b.OutboxMessages', N'U') IS NULL
    BEGIN
        CREATE TABLE [b2b].[OutboxMessages]
        (
            [Id]            [uniqueidentifier]   NOT NULL,
            [RecordId]      [int] IDENTITY (1,1) NOT NULL,
            [Type]          [nvarchar](255)      NOT NULL,
            [Data]          [nvarchar](max)      NOT NULL,
            [CreationDate]  [datetime2](7)       NOT NULL,
            [ProcessedDate] [datetime2](7)       NULL,
            CONSTRAINT [PK_OutboxMessages] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    END
go

IF OBJECT_ID(N'b2b.OutgoingMessages', N'U') IS NULL
    BEGIN
        CREATE TABLE [b2b].[OutgoingMessages]
        (
            [Id]                          [uniqueidentifier]   NOT NULL,
            [RecordId]                    [int] IDENTITY (1,1) NOT NULL,
            [DocumentType]                [nvarchar](255)      NOT NULL,
            [ReceiverId]                  [nvarchar](255)      NOT NULL,
            [IsPublished]                 [bit]                NOT NULL,
            [CorrelationId]               [nvarchar](255)      NOT NULL,
            [OriginalMessageId]           [nvarchar](50)       NOT NULL,
            [ProcessType]                 [nvarchar](50)       NOT NULL,
            [ReceiverRole]                [nvarchar](50)       NOT NULL,
            [SenderId]                    [nvarchar](50)       NOT NULL,
            [SenderRole]                  [nvarchar](50)       NOT NULL,
            [MarketActivityRecordPayload] [nvarchar](max)      NOT NULL,
            [ReasonCode]                  [nvarchar](10)       NULL,
            CONSTRAINT [PK_OutgoingMessages] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

        ALTER TABLE [b2b].[OutgoingMessages]
            ADD DEFAULT ('None') FOR [CorrelationId]

        ALTER TABLE [b2b].[OutgoingMessages]
            ADD DEFAULT ('None') FOR [OriginalMessageId]

        ALTER TABLE [dbo].[OutboxMessages]
            ADD DEFAULT ('None') FOR [Correlation]
    END
go

IF OBJECT_ID(N'b2b.QueuedInternalCommands', N'U') IS NULL
    BEGIN
        CREATE TABLE [b2b].[QueuedInternalCommands]
        (
            [Id]            [uniqueidentifier]   NOT NULL,
            [RecordId]      [int] IDENTITY (1,1) NOT NULL,
            [Type]          [nvarchar](255)      NOT NULL,
            [Data]          [nvarchar](max)      NOT NULL,
            [ProcessedDate] [datetime2](1)       NULL,
            [CreationDate]  [datetime2](7)       NOT NULL,
            [ErrorMessage]  [nvarchar](max)      NULL,
            CONSTRAINT [PK_InternalCommandQueue] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
            CONSTRAINT [UC_InternalCommandQueue_Id] UNIQUE CLUSTERED
                (
                 [RecordId] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

        ALTER TABLE [dbo].[QueuedInternalCommands]
            ADD DEFAULT ('None') FOR [Correlation]

    END
go

IF OBJECT_ID(N'b2b.ReasonTranslations', N'U') IS NULL
    BEGIN
        CREATE TABLE [b2b].[ReasonTranslations]
        (
            [Id]           [uniqueidentifier]   NOT NULL,
            [RecordId]     [int] IDENTITY (1,1) NOT NULL,
            [ErrorCode]    [nvarchar](250)      NOT NULL,
            [Code]         [nvarchar](3)        NOT NULL,
            [Text]         [nvarchar](max)      NOT NULL,
            [LanguageCode] [nvarchar](2)        NOT NULL,
            CONSTRAINT [PK_ReasonTranslations] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
            CONSTRAINT [UC_ReasonTranslations_Id] UNIQUE CLUSTERED
                (
                 [RecordId] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
            CONSTRAINT [UC_Code] UNIQUE NONCLUSTERED
                (
                 [ErrorCode] ASC,
                 [LanguageCode] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    END
go

IF OBJECT_ID(N'b2b.TransactionIds', N'U') IS NULL
    BEGIN
        CREATE TABLE [b2b].[TransactionIds]
        (
            [RecordId]      [int] IDENTITY (1,1) NOT NULL,
            [TransactionId] [nvarchar](50)       NOT NULL,
            CONSTRAINT [PK_TransactionIds] PRIMARY KEY NONCLUSTERED
                (
                 [TransactionId] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
go
