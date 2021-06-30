GO
/****** Object: Table [dbo].[AccountingPoints] Script Date: 6/15/2021 1:51:00 PM ******/
CREATE TABLE [dbo].[AccountingPoints](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [GsrnNumber] [nvarchar](36) NOT NULL,
    [ProductionObligated] [bit] NOT NULL,
    [PhysicalState] [int] NOT NULL,
    [Type] [int] NOT NULL,
    [RowVersion] [timestamp] NOT NULL,
    CONSTRAINT [PK_AccountingPoints] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
    GO
/****** Object: Table [dbo].[BusinessProcesses] Script Date: 6/15/2021 1:51:00 PM ******/

CREATE TABLE [dbo].[BusinessProcesses](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [TransactionId] [nvarchar](50) NOT NULL,
    [EffectiveDate] [datetime2](7) NOT NULL,
    [ProcessType] [int] NOT NULL,
    [Status] [int] NOT NULL,
    [AccountingPointId] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_BusinessProcesses] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
    GO
/****** Object: Table [dbo].[ConsumerRegistrations] Script Date: 6/15/2021 1:51:00 PM ******/

CREATE TABLE [dbo].[ConsumerRegistrations](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [ConsumerId] [uniqueidentifier] NOT NULL,
    [BusinessProcessId] [uniqueidentifier] NOT NULL,
    [MoveInDate] [datetime2](7) NULL,
    [AccountingPointId] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_ConsumerRegistrations] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
    GO
/****** Object: Table [dbo].[Consumers] Script Date: 6/15/2021 1:51:00 PM ******/

CREATE TABLE [dbo].[Consumers](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [CvrNumber] [nvarchar](50) NULL,
    [CprNumber] [nvarchar](50) NULL,
    [Name] [nvarchar](255) NOT NULL,
    [RowVersion] [timestamp] NOT NULL,
    CONSTRAINT [PK_Consumers] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
    GO
/****** Object: Table [dbo].[EnergySuppliers] Script Date: 6/15/2021 1:51:00 PM ******/

CREATE TABLE [dbo].[EnergySuppliers](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [GlnNumber] [nvarchar](38) NOT NULL,
    [RowVersion] [timestamp] NOT NULL,
    CONSTRAINT [PK_EnergySuppliers] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
    GO
/****** Object: Table [dbo].[OutboxMessages] Script Date: 6/15/2021 1:51:00 PM ******/

CREATE TABLE [dbo].[OutboxMessages](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [Type] [nvarchar](255) NOT NULL,
    [Data] [nvarchar](max) NOT NULL,
    [Category] [nvarchar](50) NOT NULL,
    [CreationDate] [datetime2](7) NOT NULL,
    [ProcessedDate] [datetime2](7) NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    GO
/****** Object: Table [dbo].[ProcessManagers] Script Date: 6/15/2021 1:51:00 PM ******/

CREATE TABLE [dbo].[ProcessManagers](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [BusinessProcessId] [uniqueidentifier] NOT NULL,
    [EffectiveDate] [datetime2](7) NOT NULL,
    [State] [int] NOT NULL,
    [Type] [nvarchar](200) NOT NULL,
    CONSTRAINT [PK_ProcessManagers] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UC_ProcessManagers_Id] UNIQUE CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
    GO
/****** Object: Table [dbo].[QueuedInternalCommands] Script Date: 6/15/2021 1:51:00 PM ******/

CREATE TABLE [dbo].[QueuedInternalCommands](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [Type] [nvarchar](255) NOT NULL,
    [Data] [varbinary](max) NOT NULL,
    [ScheduleDate] [datetime2](1) NULL,
    [DispatchedDate] [datetime2](1) NULL,
    [SequenceId] [bigint] NULL,
    [ProcessedDate] [datetime2](1) NULL,
    [CreationDate] [datetime2](7) NOT NULL,
    [BusinessProcessId] [uniqueidentifier] NULL,
    CONSTRAINT [PK_InternalCommandQueue] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UC_InternalCommandQueue_Id] UNIQUE CLUSTERED
(
[RecordId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    GO
/****** Object: Table [dbo].[SupplierRegistrations] Script Date: 6/15/2021 1:51:00 PM ******/

CREATE TABLE [dbo].[SupplierRegistrations](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [EnergySupplierId] [uniqueidentifier] NOT NULL,
    [BusinessProcessId] [uniqueidentifier] NOT NULL,
    [StartOfSupplyDate] [datetime2](7) NULL,
    [EndOfSupplyDate] [datetime2](7) NULL,
    [AccountingPointId] [uniqueidentifier] NOT NULL,
    CONSTRAINT [PK_SupplierRegistrations] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
    GO
ALTER TABLE [dbo].[BusinessProcesses] WITH CHECK ADD CONSTRAINT [FK_BusinessProcesses_AccountingPoints] FOREIGN KEY([AccountingPointId])
    REFERENCES [dbo].[AccountingPoints] ([Id])
    GO
ALTER TABLE [dbo].[BusinessProcesses] CHECK CONSTRAINT [FK_BusinessProcesses_AccountingPoints]
    GO
ALTER TABLE [dbo].[ConsumerRegistrations] WITH CHECK ADD CONSTRAINT [FK_ConsumerRegistrations_AccountingPoints] FOREIGN KEY([AccountingPointId])
    REFERENCES [dbo].[AccountingPoints] ([Id])
    GO
ALTER TABLE [dbo].[ConsumerRegistrations] CHECK CONSTRAINT [FK_ConsumerRegistrations_AccountingPoints]
    GO
ALTER TABLE [dbo].[ConsumerRegistrations] WITH CHECK ADD CONSTRAINT [FK_ConsumerRegistrations_BusinessProcesses] FOREIGN KEY([BusinessProcessId])
    REFERENCES [dbo].[BusinessProcesses] ([Id])
    GO
ALTER TABLE [dbo].[ConsumerRegistrations] CHECK CONSTRAINT [FK_ConsumerRegistrations_BusinessProcesses]
    GO
ALTER TABLE [dbo].[ConsumerRegistrations] WITH CHECK ADD CONSTRAINT [FK_ConsumerRegistrations_Consumers] FOREIGN KEY([ConsumerId])
    REFERENCES [dbo].[Consumers] ([Id])
    GO
ALTER TABLE [dbo].[ConsumerRegistrations] CHECK CONSTRAINT [FK_ConsumerRegistrations_Consumers]
    GO
ALTER TABLE [dbo].[ProcessManagers] WITH CHECK ADD CONSTRAINT [FK_ProcessManagers_BusinessProcesses] FOREIGN KEY([BusinessProcessId])
    REFERENCES [dbo].[BusinessProcesses] ([Id])
    GO
ALTER TABLE [dbo].[ProcessManagers] CHECK CONSTRAINT [FK_ProcessManagers_BusinessProcesses]
    GO
ALTER TABLE [dbo].[SupplierRegistrations] WITH CHECK ADD CONSTRAINT [FK_SupplierRegistrations_AccountingPoints] FOREIGN KEY([AccountingPointId])
    REFERENCES [dbo].[AccountingPoints] ([Id])
    GO
ALTER TABLE [dbo].[SupplierRegistrations] CHECK CONSTRAINT [FK_SupplierRegistrations_AccountingPoints]
    GO
ALTER TABLE [dbo].[SupplierRegistrations] WITH CHECK ADD CONSTRAINT [FK_SupplierRegistrations_BusinessProcesses] FOREIGN KEY([BusinessProcessId])
    REFERENCES [dbo].[BusinessProcesses] ([Id])
    GO
ALTER TABLE [dbo].[SupplierRegistrations] CHECK CONSTRAINT [FK_SupplierRegistrations_BusinessProcesses]
    GO
ALTER TABLE [dbo].[SupplierRegistrations] WITH CHECK ADD CONSTRAINT [FK_SupplierRegistrations_EnergySuppliers] FOREIGN KEY([EnergySupplierId])
    REFERENCES [dbo].[EnergySuppliers] ([Id])
    GO
ALTER TABLE [dbo].[SupplierRegistrations] CHECK CONSTRAINT [FK_SupplierRegistrations_EnergySuppliers]
    GO