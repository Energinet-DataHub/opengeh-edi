IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'007F098E-E20C-4B6B-B114-AEB8BD9CA110') IS NULL
    BEGIN
        INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
        VALUES (N'007F098E-E20C-4B6B-B114-AEB8BD9CA110', N'EnergySupplierIdIsEmpty', N'E16', N'Elleverandør identifikation skal være udfyldt',
                N'dk');
    END
GO
IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'8819AE7A-EAAE-4BF8-84D0-E94E9063B907') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'8819AE7A-EAAE-4BF8-84D0-E94E9063B907', N'EnergySupplierIdIsEmpty', N'E16', N'Energy supplier identificaton must be part of the message',
    N'en');
END
GO
IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'76CEF313-7C3C-4F49-8958-1F1FFE7BD131') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'76CEF313-7C3C-4F49-8958-1F1FFE7BD131', N'EnergySupplierDoesNotMatchSender', N'E16', N'Der er uoverensstemmelse imellem afsender ID og elleverandørens identifikaton i meddelelsen',
    N'dk');
END
GO
IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'EF9B52B7-F3E6-46AD-B013-6A5DA393912F') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'EF9B52B7-F3E6-46AD-B013-6A5DA393912F', N'EnergySupplierDoesNotMatchSender', N'E16', N'Sender ID is not consistent with energy supplier identification',
    N'en');
END
GO
IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'653947F5-D20E-45B9-807A-6F5C6541A9CF') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'653947F5-D20E-45B9-807A-6F5C6541A9CF', N'MoveInRegisteredOnSameDateIsNotAllowed', N'E22', N'Der er allerede anmeldt en tilflytning på denne dato',
    N'dk');
END
GO
IF (SELECT Id
    FROM [b2b].[ReasonTranslations]
    WHERE Id = N'1226DE78-0F07-4C95-9108-742EE575E453') IS NULL
BEGIN
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'1226DE78-0F07-4C95-9108-742EE575E453', N'MoveInRegisteredOnSameDateIsNotAllowed', N'E22', N'There is already a move-in on this date',
    N'en');
END
GO
