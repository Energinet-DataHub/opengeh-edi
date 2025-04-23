EXEC sp_rename 'dbo.MeteringPointArchivedMessages.SenderRoleCode', SenderRole, 'COLUMN'
GO
EXEC sp_rename 'dbo.MeteringPointArchivedMessages.ReceiverRoleCode', ReceiverRole, 'COLUMN'
GO
