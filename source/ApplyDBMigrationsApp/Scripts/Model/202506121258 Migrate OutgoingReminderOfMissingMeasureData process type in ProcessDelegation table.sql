UPDATE [dbo].[ProcessDelegation]
    SET [DelegatedProcess] = 'MissingMeasurementLog'
    WHERE [DelegatedProcess] = 'OutgoingReminderOfMissingMeasureData';
