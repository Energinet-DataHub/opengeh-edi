DROP TABLE [dbo].[TransactionIds]

CREATE TABLE [b2b].[TransactionIds]
(
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [TransactionId] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_TransactionIds] PRIMARY KEY NONCLUSTERED ([TransactionId] ASC)
)