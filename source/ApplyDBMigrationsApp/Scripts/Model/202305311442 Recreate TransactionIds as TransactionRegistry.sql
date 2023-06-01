DROP TABLE [dbo].[TransactionIds]

CREATE TABLE [dbo].[TransactionRegistry](
           [RecordId]                        [int] IDENTITY (1,1) NOT NULL,
           [TransactionId]                   [VARCHAR](50) NOT NULL,
           [SenderId]                        [VARCHAR](255)     NOT NULL,
           CONSTRAINT [PK_TransactionRegistry_TransactionIdAndSenderId] PRIMARY KEY NONCLUSTERED
               ([TransactionId], [SenderId]) 
               WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];