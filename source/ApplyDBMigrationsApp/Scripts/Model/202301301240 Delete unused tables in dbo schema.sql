IF OBJECT_ID(N'dbo.SupplierRegistrations', N'U') IS NOT NULL
    DROP TABLE [dbo].[SupplierRegistrations]
go

IF OBJECT_ID(N'dbo.ProcessManagers', N'U') IS NOT NULL
    DROP TABLE [dbo].[ProcessManagers]
go

IF OBJECT_ID(N'dbo.ConsumerRegistrations', N'U') IS NOT NULL
    DROP TABLE [dbo].[ConsumerRegistrations]
go

IF OBJECT_ID(N'dbo.BusinessProcesses', N'U') IS NOT NULL
    DROP TABLE [dbo].[BusinessProcesses]
go        

IF OBJECT_ID(N'dbo.AccountingPoints', N'U') IS NOT NULL
    DROP TABLE [dbo].[AccountingPoints]
go
        
IF OBJECT_ID(N'dbo.EnergySuppliers', N'U') IS NOT NULL
    DROP TABLE [dbo].[EnergySuppliers]
go
    
IF OBJECT_ID(N'dbo.MessageHubMessages', N'U') IS NOT NULL
    DROP TABLE [dbo].[MessageHubMessages]
go
    
IF OBJECT_ID(N'dbo.OutboxMessages', N'U') IS NOT NULL
    DROP TABLE [dbo].[OutboxMessages]
go

IF OBJECT_ID(N'dbo.QueuedInternalCommands', N'U') IS NOT NULL
    DROP TABLE [dbo].[QueuedInternalCommands]
go 