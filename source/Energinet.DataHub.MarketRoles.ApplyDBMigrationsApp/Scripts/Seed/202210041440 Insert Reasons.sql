IF (SELECT Id
      FROM [b2b].[ReasonTranslations]
      WHERE Id = N'FC921BC6-17F1-45FE-839B-7AF5FAEFF3F2') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'FC921BC6-17F1-45FE-839B-7AF5FAEFF3F2', N'InvalidCvrNumber', N'D17',
    N'Invalid customer number',
    N'en');
END
GO

IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'5984ED82-C854-419F-A1FA-19DE9EF0D406') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'5984ED82-C854-419F-A1FA-19DE9EF0D406', N'InvalidCvrNumber', N'D17',
    N'Ugyldigt CPR/CVR number',
    N'dk');
END
GO

IF (SELECT Id
      FROM [b2b].[ReasonTranslations]
      WHERE Id = N'57E0CCFE-53D5-4445-B996-06B425D27A55') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'57E0CCFE-53D5-4445-B996-06B425D27A55', N'InvalidCprNumber', N'D17',
    N'Invalid customer number',
    N'en');
END
GO

IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'421386D9-0CFB-4AE3-A6FD-006822F0BA0B') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'421386D9-0CFB-4AE3-A6FD-006822F0BA0B', N'InvalidCprNumber', N'D17',
    N'Ugyldigt CPR/CVR number',
    N'dk');
END
GO