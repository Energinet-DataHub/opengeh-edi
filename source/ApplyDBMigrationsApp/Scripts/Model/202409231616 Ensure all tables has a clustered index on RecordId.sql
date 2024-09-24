-- Actor
ALTER TABLE Actor DROP COLUMN RecordId;
GO
ALTER TABLE Actor ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON Actor(RecordId)
GO

-- ActorCertificate
ALTER TABLE ActorCertificate ADD RecordId BIGINT IDENTITY(1,1);

CREATE CLUSTERED INDEX IX_RecordId ON ActorCertificate(RecordId)
GO

-- ActorMessageQueues
ALTER TABLE ActorMessageQueues DROP COLUMN RecordId;
ALTER TABLE ActorMessageQueues ADD RecordId BIGINT IDENTITY(1,1);
GO

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

ALTER TABLE AggregatedMeasureDataProcesses DROP COLUMN RecordId;
ALTER TABLE AggregatedMeasureDataProcesses ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON AggregatedMeasureDataProcesses(RecordId)
GO

-- AggregatedMeasureDataProcessGridAreas
ALTER TABLE AggregatedMeasureDataProcessGridAreas DROP CONSTRAINT UX_AggregatedMeasureDataProcessGridAreas_RecordId
ALTER TABLE AggregatedMeasureDataProcessGridAreas DROP COLUMN RecordId;
ALTER TABLE AggregatedMeasureDataProcessGridAreas ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON AggregatedMeasureDataProcessGridAreas(RecordId)
GO

-- ArchivedMessages
ALTER TABLE ArchivedMessages DROP CONSTRAINT PK_ArchivedMessages_Id
GO

ALTER TABLE ArchivedMessages ADD CONSTRAINT PK_ArchivedMessages_Id
    PRIMARY KEY NONCLUSTERED ([Id])
GO

ALTER TABLE ArchivedMessages DROP COLUMN RecordId;
ALTER TABLE ArchivedMessages ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON ArchivedMessages(RecordId)
GO

-- Bundles
ALTER TABLE Bundles DROP COLUMN RecordId;
ALTER TABLE Bundles ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON Bundles(RecordId)
GO

-- GridAreaOwner
ALTER TABLE GridAreaOwner ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON GridAreaOwner(RecordId)
GO

-- MarketDocuments
ALTER TABLE MarketDocuments DROP COLUMN RecordId;
ALTER TABLE MarketDocuments ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON MarketDocuments(RecordId)
GO

-- MarketEvaluationPoints
ALTER TABLE MarketEvaluationPoints DROP COLUMN RecordId;
ALTER TABLE MarketEvaluationPoints ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON MarketEvaluationPoints(RecordId)
GO

-- MessageRegistry
ALTER TABLE MessageRegistry DROP COLUMN RecordId;
ALTER TABLE MessageRegistry ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON MessageRegistry(RecordId)
GO

-- Outbox
ALTER TABLE Outbox DROP CONSTRAINT UX_Outbox_RecordId
ALTER TABLE Outbox DROP COLUMN RecordId;
ALTER TABLE Outbox ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON Outbox(RecordId)
GO

-- OutgoingMessages
ALTER TABLE OutgoingMessages DROP COLUMN RecordId;
ALTER TABLE OutgoingMessages ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON OutgoingMessages(RecordId)
GO

-- ProcessDelegation
ALTER TABLE ProcessDelegation DROP CONSTRAINT UX_ProcessDelegation_RecordId
ALTER TABLE ProcessDelegation DROP COLUMN RecordId;
ALTER TABLE ProcessDelegation ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON ProcessDelegation(RecordId)
GO

-- QueuedInternalCommands
ALTER TABLE QueuedInternalCommands DROP CONSTRAINT UC_InternalCommandQueue_Id
ALTER TABLE QueuedInternalCommands DROP COLUMN RecordId;
ALTER TABLE QueuedInternalCommands ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON QueuedInternalCommands(RecordId)
GO

-- ReceivedInboxEvents 
ALTER TABLE ReceivedInboxEvents DROP COLUMN RecordId;
ALTER TABLE ReceivedInboxEvents ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON ReceivedInboxEvents(RecordId)
GO

-- ReceivedIntegrationEvents
ALTER TABLE ReceivedIntegrationEvents DROP COLUMN RecordId;
ALTER TABLE ReceivedIntegrationEvents ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON ReceivedIntegrationEvents(RecordId)
GO

-- TransactionRegistry
ALTER TABLE TransactionRegistry DROP COLUMN RecordId;
ALTER TABLE TransactionRegistry ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON TransactionRegistry(RecordId)
GO

-- WholesaleServicesProcessChargeTypes
ALTER TABLE WholesaleServicesProcessChargeTypes DROP CONSTRAINT PK_WholesaleServicesProcessChargeTypes
GO

ALTER TABLE WholesaleServicesProcessChargeTypes ADD CONSTRAINT PK_WholesaleServicesProcessChargeTypes
    PRIMARY KEY NONCLUSTERED (ChargeTypeId)

ALTER TABLE WholesaleServicesProcessChargeTypes DROP COLUMN RecordId;
ALTER TABLE WholesaleServicesProcessChargeTypes ADD RecordId BIGINT IDENTITY(1,1);
GO

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

ALTER TABLE WholesaleServicesProcesses DROP COLUMN RecordId;
ALTER TABLE WholesaleServicesProcesses ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON WholesaleServicesProcesses(RecordId)

-- WholesaleServicesProcessGridAreas
ALTER TABLE WholesaleServicesProcessGridAreas DROP CONSTRAINT UX_WholesaleServicesProcessGridAreas_RecordId
ALTER TABLE WholesaleServicesProcessGridAreas DROP COLUMN RecordId;
ALTER TABLE WholesaleServicesProcessGridAreas ADD RecordId BIGINT IDENTITY(1,1);
GO

CREATE CLUSTERED INDEX IX_RecordId ON WholesaleServicesProcessGridAreas(RecordId)
GO