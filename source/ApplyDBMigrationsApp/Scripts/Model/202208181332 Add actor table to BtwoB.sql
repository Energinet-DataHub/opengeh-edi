BEGIN
CREATE TABLE [b2b].[Actor]
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