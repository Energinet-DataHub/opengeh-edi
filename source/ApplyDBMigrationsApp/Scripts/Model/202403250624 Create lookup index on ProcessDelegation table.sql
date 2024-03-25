CREATE INDEX idx_ProcessDelegations_SearchFields
    ON [ProcessDelegation] (GridAreaCode, DelegatedByActorNumber, DelegatedByActorRole, DelegatedProcess, Start);