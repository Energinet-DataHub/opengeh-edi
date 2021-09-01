ALTER TABLE OutboxMessages
ADD Correlation [nvarchar](255) NOT NULL DEFAULT('None');