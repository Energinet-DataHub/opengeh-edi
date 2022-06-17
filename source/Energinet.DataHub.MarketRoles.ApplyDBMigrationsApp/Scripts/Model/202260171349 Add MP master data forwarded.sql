ALTER TABLE [b2b].[MoveInTransactions]
    ADD 
    [ForwardedMeteringPointMasterData] [bit] CONSTRAINT DF_ForwardedMeteringPointMasterData DEFAULT 0 NOT NULL

