BEGIN
CREATE TABLE [dbo].[OutgoingMessagesConfiguration](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[ActorNumber] [nvarchar](50) NOT NULL,
	[ActorRole] [nvarchar](50) NOT NULL,
	[DocumentType] [nvarchar](100) NOT NULL,
	[OutputFormat] [nvarchar](10) NOT NULL,
 CONSTRAINT [PK_OutgoingMessagesFormat] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END	