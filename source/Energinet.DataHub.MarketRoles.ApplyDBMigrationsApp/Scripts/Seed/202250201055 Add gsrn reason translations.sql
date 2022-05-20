DELETE from [b2b].[ReasonTranslations] where ErrorCode='AccountingPointIdentifierIsRequired';

INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'F82A727B-5AC0-4366-A0A9-B64C5613A466', 'GsrnNumberIsRequired', 'D64', 'Målepunkts ID er påkrævet', 'dk');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'82798ED1-9741-4CA3-9F0E-6DC58B8B64D1', 'GsrnNumberIsRequired', 'D64', 'Metering point ID is required', 'en');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'0565A09A-794E-4653-8302-7CFA0ABAFB67', 'InvalidGsrnNumber', 'E10', 'Målepunktet er ikke et valid målepunkt, er ikke et EAN18, forkert tjeksum eller starter ikke med 57', 'dk');
INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode)
VALUES (N'12B4DB54-B22C-41AA-BA20-358F75B31FA8', 'InvalidGsrnNumber', 'E10', 'Metering point ID is not a valid GSRN/EAN18 code (wrong checksum) or does not start with digits 57', 'en');