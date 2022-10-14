ALTER TABLE b2b.MoveInTransactions
    ADD [RequestedByActorNumber] [nvarchar](50) NULL
GO
UPDATE b2b.MoveInTransactions
SET RequestedByActorNumber = 
    (SELECT ReceiverId FROM b2b.OutgoingMessages WHERE TransactionId = TransactionId AND DocumentType = 'RejectRequestChangeOfSupplier' OR DocumentType = 'ConfirmRequestChangeOfSupplier')
GO
ALTER TABLE b2b.MoveInTransactions
    ALTER COLUMN [RequestedByActorNumber] [nvarchar](50) NOT NULL