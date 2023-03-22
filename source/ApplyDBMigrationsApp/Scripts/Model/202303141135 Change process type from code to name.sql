UPDATE [dbo].[OutgoingMessages]
    SET ProcessType = 'BalanceFixing'
WHERE ProcessType = 'D04';

UPDATE [dbo].[OutgoingMessages]
    SET ProcessType = 'MoveIn'
    WHERE ProcessType = 'E65';

UPDATE [dbo].[EnqueuedMessages]
    SET ProcessType = 'BalanceFixing'
    WHERE ProcessType = 'D04';

UPDATE [dbo].[EnqueuedMessages]
    SET ProcessType = 'MoveIn'
    WHERE ProcessType = 'E65';
