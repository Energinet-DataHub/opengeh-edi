CREATE TABLE dbo.GridArea
(
    Id uniqueIdentifier NOT NULL,
    GridAreaCode nvarchar(3) NOT NULL,
    ValidFrom datetime2(7) NOT NULL,
    ActorNumber nvarchar(16) NOT NULL,
    CONSTRAINT PK_GridAreas PRIMARY KEY NONCLUSTERED (Id)
)

CREATE INDEX IX_GridAreas_GridAreaCode ON [dbo].[GridArea] ([GridAreaCode])