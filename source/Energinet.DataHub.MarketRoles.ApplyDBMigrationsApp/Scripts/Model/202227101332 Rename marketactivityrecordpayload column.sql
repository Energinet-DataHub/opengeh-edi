EXEC sp_rename '[b2b].[OutgoingMessages].[MarketActivityRecordPayload]','MessageRecord', 'COLUMN'
EXEC sp_rename '[b2b].[EnqueuedMessages].[Payload]', 'MessageRecord', 'COLUMN'