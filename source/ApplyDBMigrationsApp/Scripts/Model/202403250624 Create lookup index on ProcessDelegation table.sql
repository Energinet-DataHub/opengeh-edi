CREATE INDEX idx_ProcessDelegation_SearchFields
    ON [ProcessDelegation] (GridAreaCode, DelegatedByActorNumber, DelegatedByActorRole, DelegatedProcess, Start);