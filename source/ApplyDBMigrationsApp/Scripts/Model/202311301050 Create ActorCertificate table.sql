CREATE TABLE dbo.ActorCertificate
(
    Id              UNIQUEIDENTIFIER NOT NULL,
    ActorNumber     NVARCHAR(16) NOT NULL,
    ActorRole       NVARCHAR(3) NOT NULL,
    Thumbprint      NVARCHAR(1000) NOT NULL,
    ValidFrom       DATETIME2(7) NOT NULL,
    SequenceNumber  INT NOT NULL,
    CONSTRAINT PK_ActorCertificate PRIMARY KEY NONCLUSTERED (Id)
)
GO

CREATE UNIQUE INDEX UX_ActorCertificate_ActorNumber_ActorRole ON [dbo].[ActorCertificate] ([ActorNumber], [ActorRole])
GO

CREATE INDEX IX_ActorCertificate_Thumbprint ON [dbo].[ActorCertificate] ([Thumbprint])
GO