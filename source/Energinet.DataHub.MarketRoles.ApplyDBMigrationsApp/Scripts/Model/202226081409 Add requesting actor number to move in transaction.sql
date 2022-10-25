ALTER TABLE b2b.MoveInTransactions
    ADD [RequestedByActorNumber] [nvarchar](50) CONSTRAINT DF_RequestedByActorNumber DEFAULT '' NOT NULL
GO
UPDATE b2b.MoveInTransactions
SET RequestedByActorNumber =
        (SELECT TOP(1) ReceiverId FROM b2b.OutgoingMessages m WHERE m.TransactionId = TransactionId AND (m.DocumentType = 'RejectRequestChangeOfSupplier' OR m.DocumentType = 'ConfirmRequestChangeOfSupplier'))