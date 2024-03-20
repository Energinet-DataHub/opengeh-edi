CREATE TABLE [dbo].[MessageDelegation]
(
    [Id]                            UNIQUEIDENTIFIER NOT NULL,
    [RecordId]                      INT IDENTITY (1,1) NOT NULL,
    
    [DelegatedByActorNumber]        NVARCHAR(16) NOT NULL,
    [DelegatedByActorRole]          NVARCHAR(3) NOT NULL,
    [DelegatedToActorNumber]        NVARCHAR(16) NOT NULL,
    [DelegatedToActorRole]          NVARCHAR(3) NOT NULL,
    [GridAreaCode]                  NVARCHAR(3) NOT NULL,
    [DocumentType]                  NVARCHAR(100) NOT NULL,

    [SequenceNumber]                INT NOT NULL,
    [Start]                         DATETIMEOFFSET NOT NULL,
    [End]                           DATETIMEOFFSET NOT NULL,

    CONSTRAINT [PK_MessageDelegation]           PRIMARY KEY NONCLUSTERED ([Id]),
    CONSTRAINT [UX_MessageDelegation_RecordId]  UNIQUE CLUSTERED ([RecordId] ASC),
)
