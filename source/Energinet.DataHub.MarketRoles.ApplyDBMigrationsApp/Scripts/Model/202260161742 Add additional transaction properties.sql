ALTER TABLE [b2b].[MoveInTransactions]
    ADD 
    [StartedByMessageId] [nvarchar](50) CONSTRAINT DF_StartedByMessageId DEFAULT 'NotSet' NOT NULL,
    [NewEnergySupplierId] [nvarchar](50) CONSTRAINT DF_NewEnergySupplierId DEFAULT 'NotSet' NOT NULL,
    [ConsumerId] [nvarchar](50) NULL,
    [ConsumerName] [nvarchar](255) NULL,
    [ConsumerIdType] [nvarchar](50) NULL;

