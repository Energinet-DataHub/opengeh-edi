-- Actor
ALTER TABLE Actor ALTER COLUMN RecordId BIGINT

CREATE CLUSTERED INDEX IX_Actor_RecordId ON Actor(RecordId)
GO

-- ActorCertificate
ALTER TABLE ActorCertificate ADD RecordId BIGINT IDENTITY(1,1);

CREATE CLUSTERED INDEX IX_ActorCertificate_RecordId ON ActorCertificate(RecordId)
GO

-- ActorMessageQueues
ALTER TABLE ActorMessageQueues ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_ActorMessageQueues_RecordId ON ActorMessageQueues(RecordId)
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

ALTER TABLE AggregatedMeasureDataProcesses ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_AggregatedMeasureDataProcesses_RecordId ON AggregatedMeasureDataProcesses(RecordId)
GO

-- AggregatedMeasureDataProcessGridAreas
ALTER TABLE AggregatedMeasureDataProcessGridAreas DROP CONSTRAINT UX_AggregatedMeasureDataProcessGridAreas_RecordId
ALTER TABLE AggregatedMeasureDataProcessGridAreas ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_AggregatedMeasureDataProcessGridAreas_RecordId ON AggregatedMeasureDataProcessGridAreas(RecordId)
GO

-- ArchivedMessages
ALTER TABLE ArchivedMessages DROP CONSTRAINT PK_ArchivedMessages_Id
GO

ALTER TABLE ArchivedMessages ADD CONSTRAINT PK_ArchivedMessages_Id
    PRIMARY KEY NONCLUSTERED ([Id])

ALTER TABLE ArchivedMessages ALTER COLUMN RecordId BIGINT 
GO

CREATE CLUSTERED INDEX IX_ArchivedMessages_RecordId ON ArchivedMessages(RecordId)
GO

-- Bundles
ALTER TABLE Bundles ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_Bundles_RecordId ON Bundles(RecordId)
GO

-- GridAreaOwner
ALTER TABLE GridAreaOwner ADD RecordId BIGINT IDENTITY(1,1);

CREATE CLUSTERED INDEX IX_GridAreaOwner_RecordId ON GridAreaOwner(RecordId)
GO

-- MarketDocuments
ALTER TABLE MarketDocuments ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_MarketDocuments_RecordId ON MarketDocuments(RecordId)
GO

-- MarketEvaluationPoints
ALTER TABLE MarketEvaluationPoints ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_MarketEvaluationPoints_RecordId ON MarketEvaluationPoints(RecordId)
GO

-- MessageRegistry
ALTER TABLE MessageRegistry ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_MessageRegistry_RecordId ON MessageRegistry(RecordId)
GO

-- Outbox
ALTER TABLE Outbox DROP CONSTRAINT UX_Outbox_RecordId
ALTER TABLE Outbox ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_Outbox_RecordId ON Outbox(RecordId)
GO

-- OutgoingMessages
ALTER TABLE OutgoingMessages ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_OutgoingMessages_RecordId ON OutgoingMessages(RecordId)
GO

-- ProcessDelegation
ALTER TABLE ProcessDelegation DROP CONSTRAINT UX_ProcessDelegation_RecordId
ALTER TABLE ProcessDelegation ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_ProcessDelegation_RecordId ON ProcessDelegation(RecordId)
GO

-- QueuedInternalCommands
ALTER TABLE QueuedInternalCommands DROP CONSTRAINT UC_InternalCommandQueue_Id
ALTER TABLE QueuedInternalCommands ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_QueuedInternalCommands_RecordId ON QueuedInternalCommands(RecordId)
GO

-- ReceivedInboxEvents 
ALTER TABLE ReceivedInboxEvents ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_ReceivedInboxEvents_RecordId ON ReceivedInboxEvents(RecordId)
GO

-- ReceivedIntegrationEvents
ALTER TABLE ReceivedIntegrationEvents ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_ReceivedIntegrationEvents_RecordId ON ReceivedIntegrationEvents(RecordId)
GO

-- TransactionRegistry
ALTER TABLE TransactionRegistry ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_TransactionRegistry_RecordId ON TransactionRegistry(RecordId)
GO

-- WholesaleServicesProcessChargeTypes
ALTER TABLE WholesaleServicesProcessChargeTypes DROP CONSTRAINT PK_WholesaleServicesProcessChargeTypes

ALTER TABLE WholesaleServicesProcessChargeTypes ADD CONSTRAINT PK_WholesaleServicesProcessChargeTypes
    PRIMARY KEY NONCLUSTERED (ChargeTypeId)
GO

ALTER TABLE WholesaleServicesProcessChargeTypes ALTER COLUMN RecordId BIGINT

CREATE CLUSTERED INDEX IX_WholesaleServicesProcessChargeTypes_RecordId ON WholesaleServicesProcessChargeTypes(RecordId)
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

ALTER TABLE WholesaleServicesProcesses ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_WholesaleServicesProcesses_RecordId ON WholesaleServicesProcesses(RecordId)

-- WholesaleServicesProcessGridAreas
ALTER TABLE WholesaleServicesProcessGridAreas DROP CONSTRAINT UX_WholesaleServicesProcessGridAreas_RecordId
ALTER TABLE WholesaleServicesProcessGridAreas ALTER COLUMN RecordId BIGINT 

CREATE CLUSTERED INDEX IX_WholesaleServicesProcessGridAreas_RecordId ON WholesaleServicesProcessGridAreas(RecordId)
GO