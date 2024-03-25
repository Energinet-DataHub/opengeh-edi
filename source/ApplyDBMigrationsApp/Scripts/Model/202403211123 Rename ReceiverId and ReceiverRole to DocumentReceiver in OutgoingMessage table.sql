exec sp_rename 'dbo.OutgoingMessages.ReceiverId', DocumentReceiverNumber, 'COLUMN'
exec sp_rename 'dbo.OutgoingMessages.ReceiverRole', DocumentReceiverRole, 'COLUMN'
go
