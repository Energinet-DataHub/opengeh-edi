ALTER TABLE [b2b].[MoveInTransactions]
    ADD MeteringPointMasterDataState [nvarchar](50) NULL,
        CustomerMasterDataState [nvarchar](50) NULL,
        BusinessProcessState [nvarchar](50) NULL
GO
UPDATE [b2b].[MoveInTransactions]
SET MeteringPointMasterDataState = CASE
    WHEN ForwardedMeteringPointMasterData = 1 THEN 'Sent' ELSE 'Pending'
END,
CustomerMasterDataState = 'Pending',
BusinessProcessState = CASE
    WHEN BusinessProcessIsAccepted = 1 AND HasBusinessProcessCompleted = 1 THEN 'Completed'
    WHEN BusinessProcessIsAccepted = 1 AND HasBusinessProcessCompleted = 0 THEN 'Accepted'
    WHEN BusinessProcessIsAccepted = 0 AND State = 'Completed' THEN 'Rejected'
END
GO
ALTER TABLE [b2b].[MoveInTransactions]
    DROP CONSTRAINT DF_ForwardedMeteringPointMasterData
GO
ALTER TABLE [b2b].[MoveInTransactions]
    DROP COLUMN ForwardedMeteringPointMasterData,
    BusinessProcessIsAccepted,
    HasBusinessProcessCompleted
GO