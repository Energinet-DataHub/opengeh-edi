DELETE FROM b2b.OutgoingMessages
WHERE LEN(ReceiverId) = 36 AND IsPublished = 0