UPDATE [b2b].[EnqueuedMessages]
SET MessageCategory = 'Aggregations'
WHERE MessageCategory = 'AggregationData'