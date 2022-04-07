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