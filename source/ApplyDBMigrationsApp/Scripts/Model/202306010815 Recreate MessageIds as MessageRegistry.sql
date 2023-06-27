DROP TABLE [dbo].[MessageIds]

CREATE TABLE [dbo].[MessageRegistry](
           [RecordId]                    [int] IDENTITY (1,1) NOT NULL,
           [MessageId]                   [VARCHAR](50) NOT NULL,
           [SenderId]                    [VARCHAR](255)     NOT NULL,
           CONSTRAINT [PK_MessageRegistry_MessageIdAndSenderId] PRIMARY KEY NONCLUSTERED
               ([MessageId], [SenderId]) 
               WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];