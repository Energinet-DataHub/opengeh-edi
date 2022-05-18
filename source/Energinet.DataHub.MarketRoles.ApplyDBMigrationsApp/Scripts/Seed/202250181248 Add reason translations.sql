INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'EF0BED27-D076-495B-9819-3C55E9B1E50A', 'ConsumerNameIsRequired', 'D64', 'Kundenavn er påkrævet', 'dk');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'8A9F0DE5-5C07-4E73-977E-5225E264646A', 'ConsumerNameIsRequired', 'D64', 'Customer name is required', 'en');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'FDC0E038-6955-4F1C-8A96-19C9AEB2A58B', 'AccountingPointIdentifierIsRequired', 'D64', 'Målepunkts ID er påkrævet', 'dk');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'B0A6419A-2A4C-4B7A-9C1A-C1F78F79A540', 'AccountingPointIdentifierIsRequired', 'D64', 'Metering point ID is required', 'en');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'8CE12145-18E3-4B43-B546-02EFD07A98D9', 'UnknownAccountingPoint', 'E10', 'Målepunktet eksisterer ikke eller er ikke et forbrugs eller produktions målepunkt', 'dk');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'5DD22A72-1E16-444F-B59B-DAFC93EF1E7D', 'UnknownAccountingPoint', 'E10', 'Metering point does not exist or is not a consumption or production metering point', 'en');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'97EE4769-4357-4B33-B3EA-49F1F832281E', 'ConsumerIdentifierIsRequired', 'D64', 'Kunde ID er påkrævet', 'dk');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'BA0D36F1-EC08-4957-8EDE-130237B0171B', 'ConsumerIdentifierIsRequired', 'D64', 'Customer ID is required', 'en');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'3E1AECE5-7EFB-4E56-82CE-278EEA2CE157', 'EffectiveDateIsNotWithinAllowedTimePeriod', 'E17', 'Startdatoen er ikke modtaget indenfor den korrekte tidsperiode', 'dk');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'A3EBC763-D8B5-4C23-96C2-1B1FDB87BC56', 'EffectiveDateIsNotWithinAllowedTimePeriod', 'E17', 'The startdate is not received within the correct timeframe', 'en');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'6D608489-7267-41DC-9187-66338202E7FA', 'InvalidEffectiveDateTimeOfDay', 'D66', 'Startdato for målepunktet skal have UTC+0 med formatet YYYY-MM-DD 00:00', 'dk');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'B5AA3770-A06B-4C51-AADC-C386CF009BF2', 'InvalidEffectiveDateTimeOfDay', 'D66', 'Date time for the metering point must have UTC+0 equivalent of local format YYYY-MM-DD 00:00:00', 'en');
