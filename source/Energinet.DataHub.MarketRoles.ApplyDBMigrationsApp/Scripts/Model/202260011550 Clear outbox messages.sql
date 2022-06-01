DELETE FROM [dbo].[OutboxMessages]
WHERE ProcessedDate IS NULL
