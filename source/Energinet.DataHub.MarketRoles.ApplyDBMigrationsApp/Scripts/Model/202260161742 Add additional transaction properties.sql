ALTER TABLE [b2b].[MoveInTransactions]
    ADD 
    [StartedByMessageId] [nvarchar](50) CONSTRAINT DF_StartedByMessageId DEFAULT 'NotSet' NOT NULL

