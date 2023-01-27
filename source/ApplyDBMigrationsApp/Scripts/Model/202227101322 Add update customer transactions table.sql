CREATE TABLE [b2b].[UpdateCustomerMasterDataTransactions]
(
    [RecordId]                         [int] IDENTITY (1,1) NOT NULL,
    [TransactionId]                    [nvarchar](50)       NOT NULL,
    CONSTRAINT [PK_TransactionId] PRIMARY KEY NONCLUSTERED
    (
        [TransactionId] ASC
    ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
        ) ON [PRIMARY];