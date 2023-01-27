ALTER TABLE dbo.ConsumerRegistrations
    DROP CONSTRAINT FK_ConsumerRegistrations_Consumers
GO
ALTER TABLE dbo.ConsumerRegistrations
    DROP COLUMN ConsumerId