UPDATE b2b.MoveInTransactions
SET EndOfSupplyNotificationState = 'WasNotified'
WHERE EndOfSupplyNotificationState = 'EnergySupplierWasNotified'
GO
EXEC sp_rename 'b2b.MoveInTransactions.EndOfSupplyNotificationState', 'CurrentEnergySupplierNotificationState', 'COLUMN';