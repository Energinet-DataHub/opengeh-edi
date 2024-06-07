UPDATE [dbo].[Bundles]
SET MessageId = LOWER(MessageId)
WHERE IsDequeued = 0
  AND MessageId LIKE '%-%'