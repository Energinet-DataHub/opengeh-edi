-- Actor
CREATE CLUSTERED INDEX IX_RecordId ON Actor(RecordId)
GO

-- ActorCertificate
ALTER TABLE ActorCertificate
    ADD RecordId INT IDENTITY(1,1);

CREATE CLUSTERED INDEX IX_RecordId ON ActorCertificate(RecordId)
GO

-- ActorMessageQueues
CREATE CLUSTERED INDEX IX_RecordId ON ActorMessageQueues(RecordId)
GO

-- AggregatedMeasureDataProcesses
ALTER TABLE AggregatedMeasureDataProcessGridAreas DROP CONSTRAINT FK_AggregatedMeasureDataProcessGridAreas_AggregatedMeasureDataProcessId
ALTER TABLE AggregatedMeasureDataProcesses DROP CONSTRAINT PK_AggregatedMeasureDataProcesses
GO

ALTER TABLE AggregatedMeasureDataProcesses ADD CONSTRAINT PK_AggregatedMeasureDataProcesses
    PRIMARY KEY NONCLUSTERED (ProcessId)

ALTER TABLE AggregatedMeasureDataProcessGridAreas  WITH CHECK ADD  CONSTRAINT FK_AggregatedMeasureDataProcessGridAreas_AggregatedMeasureDataProcessId FOREIGN KEY(AggregatedMeasureDataProcessId)
    REFERENCES AggregatedMeasureDataProcesses (ProcessId)
GO

CREATE CLUSTERED INDEX IX_RecordId ON AggregatedMeasureDataProcesses(RecordId)
GO

-- ArchivedMessages
ALTER TABLE ArchivedMessages DROP CONSTRAINT PK_ArchivedMessages_Id
GO

ALTER TABLE ArchivedMessages ADD CONSTRAINT PK_ArchivedMessages_Id
    PRIMARY KEY NONCLUSTERED ([Id])

CREATE CLUSTERED INDEX IX_RecordId ON ArchivedMessages(RecordId)
GO

-- Bundles
CREATE CLUSTERED INDEX IX_RecordId ON Bundles(RecordId)
GO

-- GridAreaOwner
ALTER TABLE GridAreaOwner ADD RecordId INT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON GridAreaOwner(RecordId)
GO

-- MarketDocuments
CREATE CLUSTERED INDEX IX_RecordId ON MarketDocuments(RecordId)

-- MarketEvaluationPoints
CREATE CLUSTERED INDEX IX_RecordId ON MarketEvaluationPoints(RecordId)
GO

-- MessageRegistry
CREATE CLUSTERED INDEX IX_RecordId ON MessageRegistry(RecordId)
GO

-- OutgoingMessages
CREATE CLUSTERED INDEX IX_RecordId ON OutgoingMessages(RecordId)
GO

-- ReceivedInboxEvents 
CREATE CLUSTERED INDEX IX_RecordId ON ReceivedInboxEvents(RecordId)
GO

-- ReceivedIntegrationEvents
CREATE CLUSTERED INDEX IX_RecordId ON ReceivedIntegrationEvents(RecordId)
GO

-- TransactionRegistry
CREATE CLUSTERED INDEX IX_RecordId ON TransactionRegistry(RecordId)
GO

-- WholesaleServicesProcessChargeTypes
ALTER TABLE WholesaleServicesProcessChargeTypes DROP CONSTRAINT PK_WholesaleServicesProcessChargeTypes
GO

ALTER TABLE WholesaleServicesProcessChargeTypes ADD CONSTRAINT PK_WholesaleServicesProcessChargeTypes
    PRIMARY KEY NONCLUSTERED (ChargeTypeId)

CREATE CLUSTERED INDEX IX_RecordId ON WholesaleServicesProcessChargeTypes(RecordId)
GO

-- WholesaleServicesProcesses
ALTER TABLE WholesaleServicesProcessChargeTypes DROP CONSTRAINT FK_WholesaleServicesProcessId
ALTER TABLE WholesaleServicesProcessGridAreas DROP CONSTRAINT FK_WholesaleServicesProcessGridAreas_WholesaleServicesProcessId
ALTER TABLE WholesaleServicesProcesses DROP CONSTRAINT PK_WholesaleServicesProcesses
GO

ALTER TABLE WholesaleServicesProcesses ADD CONSTRAINT PK_WholesaleServicesProcesses
    PRIMARY KEY NONCLUSTERED (ProcessId)
ALTER TABLE WholesaleServicesProcessGridAreas  WITH CHECK ADD  CONSTRAINT [FK_WholesaleServicesProcessGridAreas_WholesaleServicesProcessId] FOREIGN KEY(WholesaleServicesProcessId)
    REFERENCES WholesaleServicesProcesses (ProcessId)
ALTER TABLE WholesaleServicesProcessChargeTypes  WITH CHECK ADD  CONSTRAINT [FK_WholesaleServicesProcessId] FOREIGN KEY(WholesaleServicesProcessId)
    REFERENCES WholesaleServicesProcesses (ProcessId)
GO

CREATE CLUSTERED INDEX IX_RecordId ON WholesaleServicesProcesses(RecordId)