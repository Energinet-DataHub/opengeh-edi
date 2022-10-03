ALTER TABLE dbo.ConsumerRegistrations
    ADD
    [CustomerName] [nvarchar](255) CONSTRAINT DF_CustomerName DEFAULT '' NOT NULL,
    [CustomerNumber] [nvarchar](50) CONSTRAINT DF_CustomerNumber DEFAULT '' NOT NULL
GO
UPDATE dbo.ConsumerRegistrations
SET
    CustomerName = (SELECT Name FROM dbo.Consumers WHERE Id = ConsumerId),
    CustomerNumber =
        (SELECT CustomerNumber =
         CASE
             WHEN CvrNumber IS NULL THEN CprNumber
             WHEN CprNumber IS NULL THEN CvrNumber
         END
         FROM dbo.Consumers WHERE Id = ConsumerId)