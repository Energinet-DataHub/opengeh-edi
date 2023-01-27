IF (SELECT Id
      FROM [b2b].[ReasonTranslations]
      WHERE Id = N'DF1C6C87-DAAC-400A-9FEB-DB1311FB7532') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'DF1C6C87-DAAC-400A-9FEB-DB1311FB7532', N'CustomerMustBeDifferentFromCurrentCustomer', N'D17',
    N'Customer number must be different from an already registered number in Datahub using an ordinary or secondary move in process',
    N'en');
END
GO

IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'2E4A29FF-7F75-439A-8265-183C71B0F713') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'2E4A29FF-7F75-439A-8265-183C71B0F713', N'CustomerMustBeDifferentFromCurrentCustomer', N'D17',
    N'CPR/ CVR skal være forskelligt for allerede registreret i DataHub for en almindelig og sekundær tilflytning',
    N'dk');
END
GO