IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'EF0BED27-D076-495B-9819-3C55E9B1E50A') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'EF0BED27-D076-495B-9819-3C55E9B1E50A', N'ConsumerNameIsRequired', N'D64', N'Kundenavn er påkrævet',
                N'dk');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'8A9F0DE5-5C07-4E73-977E-5225E264646A') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'8A9F0DE5-5C07-4E73-977E-5225E264646A', N'ConsumerNameIsRequired', N'D64',
                N'Customer name is required', N'en');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'8CE12145-18E3-4B43-B546-02EFD07A98D9') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'8CE12145-18E3-4B43-B546-02EFD07A98D9', N'UnknownAccountingPoint', N'E10',
                N'Målepunktet eksisterer ikke eller er ikke et forbrugs eller produktions målepunkt', N'dk');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'5DD22A72-1E16-444F-B59B-DAFC93EF1E7D') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'5DD22A72-1E16-444F-B59B-DAFC93EF1E7D', N'UnknownAccountingPoint', N'E10',
                N'Metering point does not exist or is not a consumption or production metering point', N'en');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'97EE4769-4357-4B33-B3EA-49F1F832281E') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'97EE4769-4357-4B33-B3EA-49F1F832281E', N'ConsumerIdentifierIsRequired', N'D64',
                N'Kunde ID er påkrævet', N'dk');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'BA0D36F1-EC08-4957-8EDE-130237B0171B') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'BA0D36F1-EC08-4957-8EDE-130237B0171B', N'ConsumerIdentifierIsRequired', N'D64',
                N'Customer ID is required', N'en');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'3E1AECE5-7EFB-4E56-82CE-278EEA2CE157') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'3E1AECE5-7EFB-4E56-82CE-278EEA2CE157', N'EffectiveDateIsNotWithinAllowedTimePeriod', N'E17',
                N'Startdatoen er ikke modtaget indenfor den korrekte tidsperiode', N'dk');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'A3EBC763-D8B5-4C23-96C2-1B1FDB87BC56') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'A3EBC763-D8B5-4C23-96C2-1B1FDB87BC56', N'EffectiveDateIsNotWithinAllowedTimePeriod', N'E17',
                N'The startdate is not received within the correct timeframe', N'en');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'6D608489-7267-41DC-9187-66338202E7FA') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'6D608489-7267-41DC-9187-66338202E7FA', N'InvalidEffectiveDateTimeOfDay', N'D66',
                N'Startdato for målepunktet skal have UTC+0 med formatet YYYY-MM-DD 00:00', N'dk');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'B5AA3770-A06B-4C51-AADC-C386CF009BF2') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'B5AA3770-A06B-4C51-AADC-C386CF009BF2', N'InvalidEffectiveDateTimeOfDay', N'D66',
                N'Date time for the metering point must have UTC+0 equivalent of local format YYYY-MM-DD 00:00:00',
                N'en');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'F82A727B-5AC0-4366-A0A9-B64C5613A466') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'F82A727B-5AC0-4366-A0A9-B64C5613A466', N'GsrnNumberIsRequired', N'D64', N'Målepunkts ID er påkrævet',
                N'dk');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'82798ED1-9741-4CA3-9F0E-6DC58B8B64D1') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'82798ED1-9741-4CA3-9F0E-6DC58B8B64D1', N'GsrnNumberIsRequired', N'D64',
                N'Metering point ID is required', N'en');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'0565A09A-794E-4653-8302-7CFA0ABAFB67') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'0565A09A-794E-4653-8302-7CFA0ABAFB67', N'InvalidGsrnNumber', N'E10',
                N'Målepunktet er ikke et valid målepunkt, er ikke et EAN18, forkert tjeksum eller starter ikke med 57',
                N'dk');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'12B4DB54-B22C-41AA-BA20-358F75B31FA8') IS NULL
    BEGIN
        INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'12B4DB54-B22C-41AA-BA20-358F75B31FA8', N'InvalidGsrnNumber', N'E10',
                N'Metering point ID is not a valid GSRN/EAN18 code (wrong checksum) or does not start with digits 57',
                N'en');
    END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'007F098E-E20C-4B6B-B114-AEB8BD9CA110') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'007F098E-E20C-4B6B-B114-AEB8BD9CA110', N'EnergySupplierIdIsEmpty', N'E16', N'Elleverandør identifikation skal være udfyldt',
    N'dk');
END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'8819AE7A-EAAE-4BF8-84D0-E94E9063B907') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'8819AE7A-EAAE-4BF8-84D0-E94E9063B907', N'EnergySupplierIdIsEmpty', N'E16', N'Energy supplier identificaton must be part of the message',
    N'en');
END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'76CEF313-7C3C-4F49-8958-1F1FFE7BD131') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'76CEF313-7C3C-4F49-8958-1F1FFE7BD131', N'EnergySupplierDoesNotMatchSender', N'E16', N'Der er uoverensstemmelse imellem afsender ID og elleverandørens identifikaton i meddelelsen',
    N'dk');
END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'EF9B52B7-F3E6-46AD-B013-6A5DA393912F') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'EF9B52B7-F3E6-46AD-B013-6A5DA393912F', N'EnergySupplierDoesNotMatchSender', N'E16', N'Sender ID is not consistent with energy supplier identification',
    N'en');
END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'653947F5-D20E-45B9-807A-6F5C6541A9CF') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'653947F5-D20E-45B9-807A-6F5C6541A9CF', N'MoveInRegisteredOnSameDateIsNotAllowed', N'E22', N'Der er allerede anmeldt en tilflytning på denne dato',
    N'dk');
END
GO
IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'1226DE78-0F07-4C95-9108-742EE575E453') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'1226DE78-0F07-4C95-9108-742EE575E453', N'MoveInRegisteredOnSameDateIsNotAllowed', N'E22', N'There is already a move-in on this date',
    N'en');
END
GO
IF (SELECT Id
      FROM [dbo].[ReasonTranslations]
      WHERE Id = N'FC921BC6-17F1-45FE-839B-7AF5FAEFF3F2') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'FC921BC6-17F1-45FE-839B-7AF5FAEFF3F2', N'InvalidCustomerNumber', N'D17',
    N'Invalid customer number',
    N'en');
END
GO

IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'5984ED82-C854-419F-A1FA-19DE9EF0D406') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'5984ED82-C854-419F-A1FA-19DE9EF0D406', N'InvalidCustomerNumber', N'D17',
    N'Ugyldigt CPR/CVR number',
    N'dk');
END
GO
IF (SELECT Id
      FROM [dbo].[ReasonTranslations]
      WHERE Id = N'DF1C6C87-DAAC-400A-9FEB-DB1311FB7532') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'DF1C6C87-DAAC-400A-9FEB-DB1311FB7532', N'CustomerMustBeDifferentFromCurrentCustomer', N'D17',
    N'Customer number must be different from an already registered number in Datahub using an ordinary or secondary move in process',
    N'en');
END
GO

IF (SELECT Id
    FROM [dbo].[ReasonTranslations]
    WHERE Id = N'2E4A29FF-7F75-439A-8265-183C71B0F713') IS NULL
BEGIN
INSERT INTO [dbo].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'2E4A29FF-7F75-439A-8265-183C71B0F713', N'CustomerMustBeDifferentFromCurrentCustomer', N'D17',
    N'CPR/ CVR skal være forskelligt for allerede registreret i DataHub for en almindelig og sekundær tilflytning',
    N'dk');
END
GO
