ALTER TABLE [b2b].[OutgoingMessages]
    ADD 
    [ReceiverRole] [nvarchar](50) NOT NULL,
    [SenderId] [nvarchar](50) NOT NULL,
    [SenderRole] [nvarchar](50) NOT NULL;

