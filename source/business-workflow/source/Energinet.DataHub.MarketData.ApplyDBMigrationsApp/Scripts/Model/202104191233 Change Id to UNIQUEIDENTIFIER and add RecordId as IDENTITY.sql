-- InternalCommandQueue --

ALTER TABLE [InternalCommandQueue]
    DROP CONSTRAINT [InternalCommandQueue_pk]

ALTER TABLE [InternalCommandQueue]
    DROP COLUMN [Id]

ALTER TABLE [InternalCommandQueue]
    ADD [RecordId] INT IDENTITY

ALTER TABLE [InternalCommandQueue]
    ADD [Id] UNIQUEIDENTIFIER

ALTER TABLE [InternalCommandQueue]
    ADD CONSTRAINT [PK_InternalCommandQueue] PRIMARY KEY ([RecordId])

ALTER TABLE [InternalCommandQueue]
    ADD CONSTRAINT [UC_InternalCommandQueue_Id] UNIQUE ([Id])


-- MarketEvaluationPoints --

ALTER TABLE [Relationships]
    DROP CONSTRAINT [Fk_Relationship_MarketEvaluationPoint]

ALTER TABLE [Relationships]
    DROP CONSTRAINT [Fk_Relationship_MarketParticipant]

ALTER TABLE [MarketEvaluationPoints]
    DROP CONSTRAINT [Pk_MarketEvaluationPoint_Id]

ALTER TABLE [MarketEvaluationPoints]
    DROP COLUMN Id

ALTER TABLE [MarketEvaluationPoints]
    ADD RecordId INT IDENTITY

ALTER TABLE [MarketEvaluationPoints]
    ADD Id UNIQUEIDENTIFIER

ALTER TABLE [MarketEvaluationPoints]
    ADD CONSTRAINT [PK_MarketEvaluationPoints] PRIMARY KEY ([RecordId])

ALTER TABLE [MarketEvaluationPoints]
    ADD CONSTRAINT [UC_MarketEvaluationPoints_Id] UNIQUE ([Id])

-- MarketParticipants --

ALTER TABLE [MarketParticipants]
    DROP CONSTRAINT [Pk_MarketParticipant_Id]

ALTER TABLE [MarketParticipants]
    DROP COLUMN Id

ALTER TABLE [MarketParticipants]
    ADD RecordId INT IDENTITY

ALTER TABLE [MarketParticipants]
    ADD Id UNIQUEIDENTIFIER

ALTER TABLE [MarketParticipants]
    ADD CONSTRAINT [PK_MarketParticipants] PRIMARY KEY ([RecordId])

ALTER TABLE [MarketParticipants]
    ADD CONSTRAINT [UC_MarketParticipants_Id] UNIQUE ([Id])


-- Relationships --

ALTER TABLE [Relationships]
    DROP COLUMN Id

ALTER TABLE [Relationships]
    ADD RecordId INT IDENTITY

ALTER TABLE [Relationships]
    ADD Id UNIQUEIDENTIFIER

ALTER TABLE [Relationships]
    ADD CONSTRAINT [PK_Relationships] PRIMARY KEY ([RecordId])

ALTER TABLE [Relationships]
    ADD CONSTRAINT [UC_Relationships_Id] UNIQUE ([Id])

ALTER TABLE [Relationships]
    DROP COLUMN [MarketParticipant_Id]

ALTER TABLE [Relationships]
    DROP COLUMN [MarketEvaluationPoint_Id]

ALTER TABLE [Relationships]
    ADD [MarketParticipant_Id] UNIQUEIDENTIFIER

ALTER TABLE [Relationships]
    ADD [MarketEvaluationPoint_Id] UNIQUEIDENTIFIER

ALTER TABLE [Relationships]
    ADD CONSTRAINT [FK_Relationships_MarketParticipant] FOREIGN KEY ([MarketParticipant_Id]) REFERENCES [MarketParticipants] ([Id])

ALTER TABLE [Relationships]
    ADD CONSTRAINT [FK_Relationships_MarketEvaluationPoint] FOREIGN KEY ([MarketEvaluationPoint_Id]) REFERENCES [MarketEvaluationPoints] ([Id])

-- OutgoingActorMessages --

ALTER TABLE [OutgoingActorMessages]
    DROP COLUMN Id

ALTER TABLE [OutgoingActorMessages]
    ADD RecordId INT IDENTITY

ALTER TABLE [OutgoingActorMessages]
    ADD Id UNIQUEIDENTIFIER

ALTER TABLE [OutgoingActorMessages]
    ADD CONSTRAINT [PK_OutgoingActorMessages] PRIMARY KEY ([RecordId])

ALTER TABLE [OutgoingActorMessages]
    ADD CONSTRAINT [UC_OutgoingActorMessages_Id] UNIQUE ([Id])