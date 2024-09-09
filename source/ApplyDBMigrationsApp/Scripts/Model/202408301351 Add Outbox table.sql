CREATE TABLE [dbo].[Outbox]
(
    [Id]                                UNIQUEIDENTIFIER NOT NULL,
    [RecordId]                          INT IDENTITY (1,1) NOT NULL,
    [CreatedBy]                         NVARCHAR(100) NOT NULL,
    [CreatedAt]                         DATETIME2 NOT NULL,
    [ModifiedBy]                        NVARCHAR(100) NULL,
    [ModifiedAt]                        DATETIME2 NULL,

    -- ROWVERSION makes Entity Framework throw an exception if trying to update a row which has already been updated (concurrency conflict)
    -- https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=data-annotations
    [RowVersion]                        ROWVERSION NOT NULL,
    [Type]                              NVARCHAR(255) NOT NULL,
    [Payload]                           NVARCHAR(MAX) NOT NULL,
    [ProcessingAt]                      DATETIME2 NULL,
    [PublishedAt]                       DATETIME2 NULL,
    [FailedAt]                          DATETIME2 NULL,
    [ErrorMessage]                      NVARCHAR(MAX) NULL,
    [ErrorCount]                        INT NOT NULL,

    CONSTRAINT [PK_Outbox]              PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_Outbox_RecordId]     UNIQUE CLUSTERED ([RecordId] ASC),
)
