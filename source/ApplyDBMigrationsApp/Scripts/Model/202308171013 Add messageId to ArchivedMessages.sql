ALTER TABLE [dbo].[ArchivedMessages]
ADD MessageId nvarchar(36) null
GO

UPDATE [dbo].[ArchivedMessages]
SET [MessageId] = [dbo].[ArchivedMessages].[Id]
GO