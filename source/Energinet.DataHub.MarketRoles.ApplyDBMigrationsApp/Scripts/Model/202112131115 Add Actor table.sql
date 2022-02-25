CREATE TABLE [dbo].[Actor]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [RecordId] INT IDENTITY(1,1) NOT NULL,
    [IdentificationNumber] nvarchar(50) NOT NULL,
    [IdentificationType] nvarchar(50) NOT NULL,
    [Roles] nvarchar(max) NOT NULL,

    CONSTRAINT [PK_Actor] PRIMARY KEY NONCLUSTERED ([Id])
)
