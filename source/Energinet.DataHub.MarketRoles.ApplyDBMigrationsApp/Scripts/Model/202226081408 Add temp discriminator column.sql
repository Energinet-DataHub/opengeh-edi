ALTER TABLE b2b.OutgoingMessages
    ADD [Discriminator] [nvarchar](100) CONSTRAINT DF_Discriminator DEFAULT 'OutgoingMessage' NOT NULL