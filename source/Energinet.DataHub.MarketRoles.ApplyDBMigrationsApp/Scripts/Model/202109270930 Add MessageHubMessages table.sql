CREATE TABLE [dbo].[MessageHubMessages]
(
    [Id] UNIQUEIDENTIFIER NOT NULL,
    [RecordId] INT IDENTITY(1,1) NOT NULL,
    [Correlation] NVARCHAR(500) NOT NULL,
    [Type] NVARCHAR(500) NOT NULL,
    [Date] [DATETIME2](7) NOT NULL,
    [Recipient] NVARCHAR(128) NOT NULL,
    BundleId [NVARCHAR](50) NULL,
    DequeuedDate [DATETIME2](7) NULL,
    GsrnNumber [NVARCHAR](36) NOT NULL,
    [Content] NVARCHAR(MAX) NOT NULL

    CONSTRAINT [PK_MessageHubMessages] PRIMARY KEY NONCLUSTERED ([Id])
)

CREATE UNIQUE CLUSTERED INDEX CIX_MessageHubMessages ON MessageHubMessages([RecordId])
