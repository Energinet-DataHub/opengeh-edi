ALTER TABLE dbo.OutgoingActorMessages
    ADD Recipient nvarchar(50) NULL
GO

UPDATE dbo.OutgoingActorMessages SET Recipient = 5790001687137
GO

ALTER TABLE dbo.OutgoingActorMessages
    ALTER COLUMN Recipient nvarchar(50) NOT NULL

GO

ALTER TABLE OutgoingActorMessages
    ADD CONSTRAINT OutgoingActorMessages_MarketParticipants_MrId_fk
        FOREIGN KEY (Recipient) REFERENCES MarketParticipants (MrId)
GO
