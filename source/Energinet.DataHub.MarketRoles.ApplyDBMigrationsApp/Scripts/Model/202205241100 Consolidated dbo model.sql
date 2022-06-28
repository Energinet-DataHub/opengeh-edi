DELETE
FROM SchemaVersions
WHERE ScriptName like 'Energinet.DataHub.MeteringPoints.ApplyDBMigrationsApp.Scripts.Model%';

IF OBJECT_ID(N'dbo.AccountingPoints', N'U') IS NULL
    BEGIN
        create table AccountingPoints
        (
            Id                  uniqueidentifier not null
                constraint PK_AccountingPoints
                    primary key nonclustered,
            RecordId            int identity,
            GsrnNumber          nvarchar(36)     not null,
            ProductionObligated bit              not null,
            PhysicalState       int              not null,
            Type                int              not null,
            RowVersion          timestamp        not null
        )
    END
go

IF OBJECT_ID(N'dbo.Actor', N'U') IS NULL
    BEGIN
        create table Actor
        (
            Id                   uniqueidentifier not null
                constraint PK_Actor
                    primary key nonclustered,
            RecordId             int identity,
            IdentificationNumber nvarchar(50)     not null,
            IdentificationType   nvarchar(50)     not null,
            Roles                nvarchar(max)    not null
        )
    END
go

IF OBJECT_ID(N'dbo.BusinessProcesses', N'U') IS NULL
    BEGIN
        create table BusinessProcesses
        (
            Id                uniqueidentifier not null
                constraint PK_BusinessProcesses
                    primary key nonclustered,
            RecordId          int identity,
            EffectiveDate     datetime2        not null,
            ProcessType       int              not null,
            Status            int              not null,
            AccountingPointId uniqueidentifier not null
                constraint FK_BusinessProcesses_AccountingPoints
                    references AccountingPoints
        )
    END
go

IF OBJECT_ID(N'dbo.Consumers', N'U') IS NULL
    BEGIN
        create table Consumers
        (
            Id         uniqueidentifier not null
                constraint PK_Consumers
                    primary key nonclustered,
            RecordId   int identity,
            CvrNumber  nvarchar(50),
            CprNumber  nvarchar(50),
            Name       nvarchar(255)    not null,
            RowVersion timestamp        not null
        )
    END
go

IF OBJECT_ID(N'dbo.ConsumerRegistrations', N'U') IS NULL
    BEGIN
        create table ConsumerRegistrations
        (
            Id                uniqueidentifier not null
                constraint PK_ConsumerRegistrations
                    primary key nonclustered,
            RecordId          int identity,
            ConsumerId        uniqueidentifier not null
                constraint FK_ConsumerRegistrations_Consumers
                    references Consumers,
            BusinessProcessId uniqueidentifier not null
                constraint FK_ConsumerRegistrations_BusinessProcesses
                    references BusinessProcesses,
            MoveInDate        datetime2,
            AccountingPointId uniqueidentifier not null
                constraint FK_ConsumerRegistrations_AccountingPoints
                    references AccountingPoints
        )
    END
go

IF OBJECT_ID(N'dbo.EnergySuppliers', N'U') IS NULL
    BEGIN
        create table EnergySuppliers
        (
            Id         uniqueidentifier not null
                constraint PK_EnergySuppliers
                    primary key nonclustered,
            RecordId   int identity,
            GlnNumber  nvarchar(38)     not null,
            RowVersion timestamp        not null
        )
    END
go

IF OBJECT_ID(N'dbo.MessageHubMessages', N'U') IS NULL
    BEGIN
        create table MessageHubMessages
        (
            Id           uniqueidentifier not null
                constraint PK_MessageHubMessages
                    primary key nonclustered,
            RecordId     int identity,
            Correlation  nvarchar(500)    not null,
            Type         nvarchar(500)    not null,
            Date         datetime2        not null,
            Recipient    nvarchar(128)    not null,
            BundleId     nvarchar(50),
            DequeuedDate datetime2,
            GsrnNumber   nvarchar(36)     not null,
            Content      nvarchar(max)    not null
        )

        create unique clustered index CIX_MessageHubMessages
            on MessageHubMessages (RecordId)
    END
go

IF OBJECT_ID(N'dbo.OutboxMessages', N'U') IS NULL
    BEGIN
        create table OutboxMessages
        (
            Id            uniqueidentifier             not null
                constraint PK_OutboxMessages
                    primary key nonclustered,
            RecordId      int identity,
            Type          nvarchar(255)                not null,
            Data          nvarchar(max)                not null,
            Category      nvarchar(50)                 not null,
            CreationDate  datetime2                    not null,
            ProcessedDate datetime2,
            Correlation   nvarchar(255) default 'None' not null
        )
    END
go

IF OBJECT_ID(N'dbo.ProcessManagers', N'U') IS NULL
    BEGIN
        create table ProcessManagers
        (
            Id                uniqueidentifier not null
                constraint PK_ProcessManagers
                    primary key nonclustered
                constraint UC_ProcessManagers_Id
                    unique clustered,
            RecordId          int identity,
            BusinessProcessId uniqueidentifier not null
                constraint FK_ProcessManagers_BusinessProcesses
                    references BusinessProcesses,
            EffectiveDate     datetime2        not null,
            State             int              not null,
            Type              nvarchar(200)    not null
        )
    END
go

IF OBJECT_ID(N'dbo.QueuedInternalCommands', N'U') IS NULL
    BEGIN
        create table QueuedInternalCommands
        (
            Id                uniqueidentifier             not null
                constraint PK_InternalCommandQueue
                    primary key nonclustered,
            RecordId          int identity
                constraint UC_InternalCommandQueue_Id
                    unique clustered,
            Type              nvarchar(255)                not null,
            Data              varbinary(max)               not null,
            ScheduleDate      datetime2(1),
            DispatchedDate    datetime2(1),
            SequenceId        bigint,
            ProcessedDate     datetime2(1),
            CreationDate      datetime2                    not null,
            BusinessProcessId uniqueidentifier,
            Correlation       nvarchar(255) default 'None' not null
        )
    END
go

IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
    BEGIN
        create table SchemaVersions
        (
            Id         int identity
                constraint PK_SchemaVersions_Id
                    primary key,
            ScriptName nvarchar(255) not null,
            Applied    datetime      not null
        )
    END
go

IF OBJECT_ID(N'dbo.SupplierRegistrations', N'U') IS NULL
    BEGIN
        create table SupplierRegistrations
        (
            Id                uniqueidentifier not null
                constraint PK_SupplierRegistrations
                    primary key nonclustered,
            RecordId          int identity,
            EnergySupplierId  uniqueidentifier not null
                constraint FK_SupplierRegistrations_EnergySuppliers
                    references EnergySuppliers,
            BusinessProcessId uniqueidentifier not null
                constraint FK_SupplierRegistrations_BusinessProcesses
                    references BusinessProcesses,
            StartOfSupplyDate datetime2,
            EndOfSupplyDate   datetime2,
            AccountingPointId uniqueidentifier not null
                constraint FK_SupplierRegistrations_AccountingPoints
                    references AccountingPoints
        )
    END
go
