ALTER TABLE [dbo].[MarketDocuments] ADD [UploadedDocumentReference] NVARCHAR(1000) NULL
GO

UPDATE [dbo].[MarketDocuments]
    SET MarketDocuments.UploadedDocumentReference = ''
    WHERE MarketDocuments.UploadedDocumentReference IS NULL
GO
    
ALTER TABLE [dbo].[MarketDocuments]
    ALTER COLUMN UploadedDocumentReference NVARCHAR(1000) NOT NULL
GO