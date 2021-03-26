ALTER TABLE [dbo].[OutgoingActorMessages]
ADD Grouping UNIQUEIDENTIFIER NULL
,Priority INTEGER NULL
GO

UPDATE [dbo].[OutgoingActorMessages] SET Grouping = NEWID(), Priority = 1
GO

Alter Table [dbo].[OutgoingActorMessages]
Alter Column Grouping UNIQUEIDENTIFIER NOT NULL
GO

Alter Table [dbo].[OutgoingActorMessages]
Alter Column Priority INTEGER NOT NULL
GO