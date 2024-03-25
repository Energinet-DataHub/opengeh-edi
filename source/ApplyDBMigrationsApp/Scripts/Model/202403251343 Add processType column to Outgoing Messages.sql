ALTER TABLE [dbo].[OutgoingMessages] 
    Add [MessageCreatedFromProcess] [nvarchar](100) NULL;

go

UPDATE [dbo].[OutgoingMessages]
SET [MessageCreatedFromProcess] = 'CalculatedEnergyResults'
WHERE [DocumentType] = 'NotifyAggregatedMeasureData' OR [DocumentType] = 'RejectRequestAggregatedMeasureData';

UPDATE [dbo].[OutgoingMessages]
SET [MessageCreatedFromProcess] = 'CalculatedWholesaleResults'
WHERE [DocumentType] = 'NotifyWholesaleServices' OR [DocumentType] = 'RejectRequestWholesaleSettlement';

go

ALTER TABLE [dbo].[OutgoingMessages]
ALTER COLUMN[MessageCreatedFromProcess] [nvarchar](100) NOT NULL;