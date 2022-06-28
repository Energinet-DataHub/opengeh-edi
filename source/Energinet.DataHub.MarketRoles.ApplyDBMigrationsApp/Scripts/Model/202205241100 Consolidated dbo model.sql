DELETE
FROM SchemaVersions
WHERE ScriptName like 'Energinet.DataHub.MarketRoles.ApplyDBMigrationsApp.Scripts.Model%';
go


IF OBJECT_ID(N'dbo.AccountingPoints', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[AccountingPoints]
        (
            [Id]                  [uniqueidentifier]   NOT NULL,
            [RecordId]            [int] IDENTITY (1,1) NOT NULL,
            [GsrnNumber]          [nvarchar](36)       NOT NULL,
            [ProductionObligated] [bit]                NOT NULL,
            [PhysicalState]       [int]                NOT NULL,
            [Type]                [int]                NOT NULL,
            [RowVersion]          [timestamp]          NOT NULL,
            CONSTRAINT [PK_AccountingPoints] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
go

IF OBJECT_ID(N'dbo.Actor', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[Actor]
        (
            [Id]                   [uniqueidentifier]   NOT NULL,
            [RecordId]             [int] IDENTITY (1,1) NOT NULL,
            [IdentificationNumber] [nvarchar](50)       NOT NULL,
            [IdentificationType]   [nvarchar](50)       NOT NULL,
            [Roles]                [nvarchar](max)      NOT NULL,
            CONSTRAINT [PK_Actor] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    END
go

IF OBJECT_ID(N'dbo.BusinessProcesses', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[BusinessProcesses]
        (
            [Id]                [uniqueidentifier]   NOT NULL,
            [RecordId]          [int] IDENTITY (1,1) NOT NULL,
            [EffectiveDate]     [datetime2](7)       NOT NULL,
            [ProcessType]       [int]                NOT NULL,
            [Status]            [int]                NOT NULL,
            [AccountingPointId] [uniqueidentifier]   NOT NULL,
            CONSTRAINT [PK_BusinessProcesses] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]

        ALTER TABLE [dbo].[BusinessProcesses]
            WITH CHECK ADD CONSTRAINT [FK_BusinessProcesses_AccountingPoints] FOREIGN KEY ([AccountingPointId])
                REFERENCES [dbo].[AccountingPoints] ([Id])

        ALTER TABLE [dbo].[BusinessProcesses]
            CHECK CONSTRAINT [FK_BusinessProcesses_AccountingPoints]

    END
go

IF OBJECT_ID(N'dbo.Consumers', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[Consumers]
        (
            [Id]         [uniqueidentifier]   NOT NULL,
            [RecordId]   [int] IDENTITY (1,1) NOT NULL,
            [CvrNumber]  [nvarchar](50)       NULL,
            [CprNumber]  [nvarchar](50)       NULL,
            [Name]       [nvarchar](255)      NOT NULL,
            [RowVersion] [timestamp]          NOT NULL,
            CONSTRAINT [PK_Consumers] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
go

IF OBJECT_ID(N'dbo.ConsumerRegistrations', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ConsumerRegistrations]
        (
            [Id]                [uniqueidentifier]   NOT NULL,
            [RecordId]          [int] IDENTITY (1,1) NOT NULL,
            [ConsumerId]        [uniqueidentifier]   NOT NULL,
            [BusinessProcessId] [uniqueidentifier]   NOT NULL,
            [MoveInDate]        [datetime2](7)       NULL,
            [AccountingPointId] [uniqueidentifier]   NOT NULL,
            CONSTRAINT [PK_ConsumerRegistrations] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
        ALTER TABLE [dbo].[ConsumerRegistrations]
            WITH CHECK ADD CONSTRAINT [FK_ConsumerRegistrations_AccountingPoints] FOREIGN KEY ([AccountingPointId])
                REFERENCES [dbo].[AccountingPoints] ([Id])

        ALTER TABLE [dbo].[ConsumerRegistrations]
            CHECK CONSTRAINT [FK_ConsumerRegistrations_AccountingPoints]

        ALTER TABLE [dbo].[ConsumerRegistrations]
            WITH CHECK ADD CONSTRAINT [FK_ConsumerRegistrations_BusinessProcesses] FOREIGN KEY ([BusinessProcessId])
                REFERENCES [dbo].[BusinessProcesses] ([Id])

        ALTER TABLE [dbo].[ConsumerRegistrations]
            CHECK CONSTRAINT [FK_ConsumerRegistrations_BusinessProcesses]

        ALTER TABLE [dbo].[ConsumerRegistrations]
            WITH CHECK ADD CONSTRAINT [FK_ConsumerRegistrations_Consumers] FOREIGN KEY ([ConsumerId])
                REFERENCES [dbo].[Consumers] ([Id])

        ALTER TABLE [dbo].[ConsumerRegistrations]
            CHECK CONSTRAINT [FK_ConsumerRegistrations_Consumers]

    END
go

IF OBJECT_ID(N'dbo.EnergySuppliers', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[EnergySuppliers]
        (
            [Id]         [uniqueidentifier]   NOT NULL,
            [RecordId]   [int] IDENTITY (1,1) NOT NULL,
            [GlnNumber]  [nvarchar](38)       NOT NULL,
            [RowVersion] [timestamp]          NOT NULL,
            CONSTRAINT [PK_EnergySuppliers] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
go

IF OBJECT_ID(N'dbo.MessageHubMessages', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[MessageHubMessages]
        (
            [Id]           [uniqueidentifier]   NOT NULL,
            [RecordId]     [int] IDENTITY (1,1) NOT NULL,
            [Correlation]  [nvarchar](500)      NOT NULL,
            [Type]         [nvarchar](500)      NOT NULL,
            [Date]         [datetime2](7)       NOT NULL,
            [Recipient]    [nvarchar](128)      NOT NULL,
            [BundleId]     [nvarchar](50)       NULL,
            [DequeuedDate] [datetime2](7)       NULL,
            [GsrnNumber]   [nvarchar](36)       NOT NULL,
            [Content]      [nvarchar](max)      NOT NULL,
            CONSTRAINT [PK_MessageHubMessages] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    END
go

IF OBJECT_ID(N'dbo.OutboxMessages', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[OutboxMessages]
        (
            [Id]            [uniqueidentifier]   NOT NULL,
            [RecordId]      [int] IDENTITY (1,1) NOT NULL,
            [Type]          [nvarchar](255)      NOT NULL,
            [Data]          [nvarchar](max)      NOT NULL,
            [Category]      [nvarchar](50)       NOT NULL,
            [CreationDate]  [datetime2](7)       NOT NULL,
            [ProcessedDate] [datetime2](7)       NULL,
            [Correlation]   [nvarchar](255)      NOT NULL,
            CONSTRAINT [PK_OutboxMessages] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    END
go

IF OBJECT_ID(N'dbo.ProcessManagers', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[ProcessManagers]
        (
            [Id]                [uniqueidentifier]   NOT NULL,
            [RecordId]          [int] IDENTITY (1,1) NOT NULL,
            [BusinessProcessId] [uniqueidentifier]   NOT NULL,
            [EffectiveDate]     [datetime2](7)       NOT NULL,
            [State]             [int]                NOT NULL,
            [Type]              [nvarchar](200)      NOT NULL,
            CONSTRAINT [PK_ProcessManagers] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
            CONSTRAINT [UC_ProcessManagers_Id] UNIQUE CLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]

        ALTER TABLE [dbo].[ProcessManagers]
            WITH CHECK ADD CONSTRAINT [FK_ProcessManagers_BusinessProcesses] FOREIGN KEY ([BusinessProcessId])
                REFERENCES [dbo].[BusinessProcesses] ([Id])

        ALTER TABLE [dbo].[ProcessManagers]
            CHECK CONSTRAINT [FK_ProcessManagers_BusinessProcesses]
    END
go

IF OBJECT_ID(N'dbo.QueuedInternalCommands', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[QueuedInternalCommands]
        (
            [Id]                [uniqueidentifier]   NOT NULL,
            [RecordId]          [int] IDENTITY (1,1) NOT NULL,
            [Type]              [nvarchar](255)      NOT NULL,
            [Data]              [varbinary](max)     NOT NULL,
            [ScheduleDate]      [datetime2](1)       NULL,
            [DispatchedDate]    [datetime2](1)       NULL,
            [SequenceId]        [bigint]             NULL,
            [ProcessedDate]     [datetime2](1)       NULL,
            [CreationDate]      [datetime2](7)       NOT NULL,
            [BusinessProcessId] [uniqueidentifier]   NULL,
            [Correlation]       [nvarchar](255)      NOT NULL,
            CONSTRAINT [PK_InternalCommandQueue] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
            CONSTRAINT [UC_InternalCommandQueue_Id] UNIQUE CLUSTERED
                (
                 [RecordId] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    END
go

IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[SchemaVersions]
        (
            [Id]         [int] IDENTITY (1,1) NOT NULL,
            [ScriptName] [nvarchar](255)      NOT NULL,
            [Applied]    [datetime]           NOT NULL,
            CONSTRAINT [PK_SchemaVersions_Id] PRIMARY KEY CLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]
    END
go

IF OBJECT_ID(N'dbo.SupplierRegistrations', N'U') IS NULL
    BEGIN
        CREATE TABLE [dbo].[SupplierRegistrations]
        (
            [Id]                [uniqueidentifier]   NOT NULL,
            [RecordId]          [int] IDENTITY (1,1) NOT NULL,
            [EnergySupplierId]  [uniqueidentifier]   NOT NULL,
            [BusinessProcessId] [uniqueidentifier]   NOT NULL,
            [StartOfSupplyDate] [datetime2](7)       NULL,
            [EndOfSupplyDate]   [datetime2](7)       NULL,
            [AccountingPointId] [uniqueidentifier]   NOT NULL,
            CONSTRAINT [PK_SupplierRegistrations] PRIMARY KEY NONCLUSTERED
                (
                 [Id] ASC
                    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY]

        ALTER TABLE [dbo].[SupplierRegistrations]
            WITH CHECK ADD CONSTRAINT [FK_SupplierRegistrations_AccountingPoints] FOREIGN KEY ([AccountingPointId])
                REFERENCES [dbo].[AccountingPoints] ([Id])

        ALTER TABLE [dbo].[SupplierRegistrations]
            CHECK CONSTRAINT [FK_SupplierRegistrations_AccountingPoints]

        ALTER TABLE [dbo].[SupplierRegistrations]
            WITH CHECK ADD CONSTRAINT [FK_SupplierRegistrations_BusinessProcesses] FOREIGN KEY ([BusinessProcessId])
                REFERENCES [dbo].[BusinessProcesses] ([Id])

        ALTER TABLE [dbo].[SupplierRegistrations]
            CHECK CONSTRAINT [FK_SupplierRegistrations_BusinessProcesses]

        ALTER TABLE [dbo].[SupplierRegistrations]
            WITH CHECK ADD CONSTRAINT [FK_SupplierRegistrations_EnergySuppliers] FOREIGN KEY ([EnergySupplierId])
                REFERENCES [dbo].[EnergySuppliers] ([Id])

        ALTER TABLE [dbo].[SupplierRegistrations]
            CHECK CONSTRAINT [FK_SupplierRegistrations_EnergySuppliers]
    END
GO
