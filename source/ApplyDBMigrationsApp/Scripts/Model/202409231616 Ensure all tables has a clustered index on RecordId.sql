-- Actor
CREATE CLUSTERED INDEX IX_RecordId ON Actor(RecordId)

go
-- ActorCertificate
ALTER TABLE ActorCertificate
    ADD RecordId int not null;

CREATE CLUSTERED INDEX IX_RecordId ON ActorCertificate(RecordId)
go

-- ActorMessageQueues
CREATE CLUSTERED INDEX IX_RecordId ON ActorMessageQueues(RecordId)
go

-- AggregatedMeasureDataProcesses
ALTER TABLE AggregatedMeasureDataProcessGridAreas DROP CONSTRAINT FK_AggregatedMeasureDataProcessGridAreas_AggregatedMeasureDataProcessId
ALTER TABLE AggregatedMeasureDataProcesses DROP CONSTRAINT PK_AggregatedMeasureDataProcesses
    go
ALTER TABLE AggregatedMeasureDataProcesses ADD CONSTRAINT PK_AggregatedMeasureDataProcesses
    PRIMARY KEY NONCLUSTERED (ProcessId)

ALTER TABLE AggregatedMeasureDataProcessGridAreas  WITH CHECK ADD  CONSTRAINT FK_AggregatedMeasureDataProcessGridAreas_AggregatedMeasureDataProcessId FOREIGN KEY(AggregatedMeasureDataProcessId)
    REFERENCES AggregatedMeasureDataProcesses (ProcessId)
    go

CREATE CLUSTERED INDEX IX_RecordId ON AggregatedMeasureDataProcesses(RecordId)
go

-- AggregatedMeasureDataProcessGridAreas
-- LGTM

-- ArchivedMessages
ALTER TABLE ArchivedMessages DROP CONSTRAINT PK_ArchivedMessages_Id
    go
ALTER TABLE ArchivedMessages ADD CONSTRAINT PK_ArchivedMessages_Id
    PRIMARY KEY NONCLUSTERED ([Id])

CREATE CLUSTERED INDEX IX_RecordId ON ArchivedMessages(RecordId)

-- Bundles
CREATE CLUSTERED INDEX IX_RecordId ON Bundles(RecordId)

-- GridAreaOwner
ALTER TABLE GridAreaOwner ADD RecordId int null;
go
WITH cte AS (
    SELECT ROW_NUMBER() OVER (order by SequenceNumber desc) AS rn, Id FROM GridAreaOwner 
)
UPDATE GridAreaOwner
SET RecordId = cte.rn
    FROM cte
WHERE GridAreaOwner.Id = cte.Id;
go

CREATE CLUSTERED INDEX IX_RecordId ON GridAreaOwner(RecordId)
go 

-- MarketDocuments
CREATE CLUSTERED INDEX IX_RecordId ON MarketDocuments(RecordId)

-- MarketEvaluationPoints
CREATE CLUSTERED INDEX IX_RecordId ON MarketEvaluationPoints(RecordId)

-- MessageRegistry
CREATE CLUSTERED INDEX IX_RecordId ON MessageRegistry(RecordId)

-- Outbox
-- LGTM

-- OutgoingMessages
CREATE CLUSTERED INDEX IX_RecordId ON OutgoingMessages(RecordId)

-- ProcessDelegation
-- LGTM

-- QueuedInternalCommands
-- LGTM

-- ReceivedInboxEvents 
CREATE CLUSTERED INDEX IX_RecordId ON ReceivedInboxEvents(RecordId)

-- ReceivedIntegrationEvents
CREATE CLUSTERED INDEX IX_RecordId ON ReceivedIntegrationEvents(RecordId)

-- TransactionRegistry
CREATE CLUSTERED INDEX IX_RecordId ON TransactionRegistry(RecordId)

-- WholesaleServicesProcessChargeTypes
ALTER TABLE WholesaleServicesProcessChargeTypes DROP CONSTRAINT PK_WholesaleServicesProcessChargeTypes
    go
ALTER TABLE WholesaleServicesProcessChargeTypes ADD CONSTRAINT PK_WholesaleServicesProcessChargeTypes
    PRIMARY KEY NONCLUSTERED (ChargeTypeId)

CREATE CLUSTERED INDEX IX_RecordId ON WholesaleServicesProcessChargeTypes(RecordId)

-- WholesaleServicesProcesses
ALTER TABLE WholesaleServicesProcessChargeTypes DROP CONSTRAINT FK_WholesaleServicesProcessId
ALTER TABLE WholesaleServicesProcessGridAreas DROP CONSTRAINT FK_WholesaleServicesProcessGridAreas_WholesaleServicesProcessId
ALTER TABLE WholesaleServicesProcesses DROP CONSTRAINT PK_WholesaleServicesProcesses
    go

ALTER TABLE WholesaleServicesProcesses ADD CONSTRAINT PK_WholesaleServicesProcesses
    PRIMARY KEY NONCLUSTERED (ProcessId)
ALTER TABLE WholesaleServicesProcessGridAreas  WITH CHECK ADD  CONSTRAINT [FK_WholesaleServicesProcessGridAreas_WholesaleServicesProcessId] FOREIGN KEY(WholesaleServicesProcessId)
    REFERENCES WholesaleServicesProcesses (ProcessId)
ALTER TABLE WholesaleServicesProcessChargeTypes  WITH CHECK ADD  CONSTRAINT [FK_WholesaleServicesProcessId] FOREIGN KEY(WholesaleServicesProcessId)
    REFERENCES WholesaleServicesProcesses (ProcessId)
    go

CREATE CLUSTERED INDEX IX_RecordId ON WholesaleServicesProcesses(RecordId) -- should delete old clustered index and create new ones

-- WholesaleServicesProcessGridAreas
-- LGTM