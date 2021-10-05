ALTER TABLE QueuedInternalCommands
    DROP COLUMN CorrelationId

ALTER TABLE QueuedInternalCommands
    ADD Correlation [nvarchar](255) NOT NULL DEFAULT('None');