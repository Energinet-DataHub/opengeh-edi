GO
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'b2b')
BEGIN
EXEC('CREATE SCHEMA [b2b] AUTHORIZATION [dbo];')
END
GO

DROP TABLE IF EXISTS [b2b].[OutboxMessages]
    GO

CREATE TABLE [b2b].[OutboxMessages](
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [Type] [nvarchar](255) NOT NULL,
    [Data] [nvarchar](max) NOT NULL,
    [CreationDate] [datetime2](7) NOT NULL,
    [ProcessedDate] [datetime2](7) NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY NONCLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    GO

DROP TABLE IF EXISTS [b2b].[OutgoingMessages]
    GO

CREATE TABLE [b2b].[OutgoingMessages]
(
    [Id] [uniqueidentifier] NOT NULL,
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [MessagePayload] [nvarchar](max) NOT NULL,
    [DocumentType] [nvarchar](255) NOT NULL,
    [RecipientId] [nvarchar](255) NOT NULL,
    [IsPublished] [bit] NOT NULL,
    CONSTRAINT [PK_OutgoingMessages] PRIMARY KEY NONCLUSTERED (
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
    GO
