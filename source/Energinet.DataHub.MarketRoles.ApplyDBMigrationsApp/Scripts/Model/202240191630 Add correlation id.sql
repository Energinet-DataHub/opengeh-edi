ALTER TABLE [b2b].[OutgoingMessages]
    ADD CorrelationId [nvarchar](255) NOT NULL DEFAULT('None');