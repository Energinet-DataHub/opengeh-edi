IF OBJECT_ID(N'b2b.MarketEvaluationPoints', N'U') IS NULL
    BEGIN
        create table b2b.MarketEvaluationPoints
        (
            Id                          uniqueidentifier not null
                constraint PK_MarketEvaluationPoints
                    primary key nonclustered,
            RecordId                    int identity,
            MarketEvaluationPointNumber nvarchar(50)     not null
                unique,
            EnergySupplierNumber        nvarchar(50)     not null
        )
    END
go

IF OBJECT_ID(N'b2b.MessageIds', N'U') IS NULL
    BEGIN
        create table b2b.MessageIds
        (
            RecordId  int identity,
            MessageId nvarchar(50) not null
                constraint PK_MessageIds
                    primary key nonclustered
        )
    END
go

IF OBJECT_ID(N'b2b.MoveInTransactions', N'U') IS NULL
    BEGIN
        create table b2b.MoveInTransactions
        (
            RecordId                         int identity,
            TransactionId                    nvarchar(50)                not null
                constraint PK_Transactions
                    primary key nonclustered,
            ProcessId                        nvarchar(50),
            MarketEvaluationPointId          nvarchar(50),
            EffectiveDate                    datetime2,
            CurrentEnergySupplierId          nvarchar(50),
            State                            nvarchar(50)
                constraint DF_State default 'Started'                    not null,
            StartedByMessageId               nvarchar(50)
                constraint DF_StartedByMessageId default 'NotSet'        not null,
            NewEnergySupplierId              nvarchar(50)
                constraint DF_NewEnergySupplierId default 'NotSet'       not null,
            ConsumerId                       nvarchar(50),
            ConsumerName                     nvarchar(255),
            ConsumerIdType                   nvarchar(50),
            ForwardedMeteringPointMasterData bit
                constraint DF_ForwardedMeteringPointMasterData default 0 not null
        )
    END
go

IF OBJECT_ID(N'b2b.OutboxMessages', N'U') IS NULL
    BEGIN
        create table b2b.OutboxMessages
        (
            Id            uniqueidentifier not null
                constraint PK_OutboxMessages
                    primary key nonclustered,
            RecordId      int identity,
            Type          nvarchar(255)    not null,
            Data          nvarchar(max)    not null,
            CreationDate  datetime2        not null,
            ProcessedDate datetime2
        )
    END
go

IF OBJECT_ID(N'b2b.OutgoingMessages', N'U') IS NULL
    BEGIN
        create table b2b.OutgoingMessages
        (
            Id                          uniqueidentifier             not null
                constraint PK_OutgoingMessages
                    primary key nonclustered,
            RecordId                    int identity,
            DocumentType                nvarchar(255)                not null,
            ReceiverId                  nvarchar(255)                not null,
            IsPublished                 bit                          not null,
            CorrelationId               nvarchar(255) default 'None' not null,
            OriginalMessageId           nvarchar(50)  default 'None' not null,
            ProcessType                 nvarchar(50)                 not null,
            ReceiverRole                nvarchar(50)                 not null,
            SenderId                    nvarchar(50)                 not null,
            SenderRole                  nvarchar(50)                 not null,
            MarketActivityRecordPayload nvarchar(max)                not null,
            ReasonCode                  nvarchar(10)
        )
    END
go

IF OBJECT_ID(N'b2b.QueuedInternalCommands', N'U') IS NULL
    BEGIN
        create table b2b.QueuedInternalCommands
        (
            Id            uniqueidentifier not null
                constraint PK_InternalCommandQueue
                    primary key nonclustered,
            RecordId      int identity
                constraint UC_InternalCommandQueue_Id
                    unique clustered,
            Type          nvarchar(255)    not null,
            Data          nvarchar(max)    not null,
            ProcessedDate datetime2(1),
            CreationDate  datetime2        not null,
            ErrorMessage  nvarchar(max)
        )
    END
go

IF OBJECT_ID(N'b2b.ReasonTranslations', N'U') IS NULL
    BEGIN
        create table b2b.ReasonTranslations
        (
            Id           uniqueidentifier not null
                constraint PK_ReasonTranslations
                    primary key nonclustered,
            RecordId     int identity
                constraint UC_ReasonTranslations_Id
                    unique clustered,
            ErrorCode    nvarchar(250)    not null,
            Code         nvarchar(3)      not null,
            Text         nvarchar(max)    not null,
            LanguageCode nvarchar(2)      not null,
            constraint UC_Code
                unique (ErrorCode, LanguageCode)
        )
    END
go

IF OBJECT_ID(N'b2b.TransactionIds', N'U') IS NULL
    BEGIN
        create table b2b.TransactionIds
        (
            RecordId      int identity,
            TransactionId nvarchar(50) not null
                constraint PK_TransactionIds
                    primary key nonclustered
        )
    END
go
