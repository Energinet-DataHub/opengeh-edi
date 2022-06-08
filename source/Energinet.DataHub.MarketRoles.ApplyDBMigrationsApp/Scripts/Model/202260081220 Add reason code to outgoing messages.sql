ALTER TABLE [b2b].[OutgoingMessages]
    ADD [ReasonCode] [nvarchar](10) NOT NULL
    GO
UPDATE [b2b].[OutgoingMessages]
SET ReasonCode =  CASE
    WHEN DocumentType = 'ConfirmRequestChangeOfSupplier' THEN 'A01'
    WHEN DocumentType = 'RejectRequestChangeOfSupplier' THEN 'A02'
END
WHERE DocumentType IN ('ConfirmRequestChangeOfSupplier', 'RejectRequestChangeOfSupplier')