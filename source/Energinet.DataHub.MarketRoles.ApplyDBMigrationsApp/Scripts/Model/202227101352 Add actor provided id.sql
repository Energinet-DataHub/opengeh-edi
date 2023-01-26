ALTER TABLE b2b.MoveInTransactions
    ADD [ActorProvidedId] nvarchar(100) NULL
GO
UPDATE b2b.MoveInTransactions
SET ActorProvidedId = TransactionId;
ALTER TABLE b2b.MoveInTransactions
ALTER COLUMN ActorProvidedId nvarchar(100) NOT NULL 
