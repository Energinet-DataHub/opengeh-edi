CREATE TABLE [dbo].[Transactions]
(
    [RecordId] [int] IDENTITY(1,1) NOT NULL,
    [TransactionId] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_Transactions] PRIMARY KEY NONCLUSTERED ([TransactionId] ASC))