    UPDATE dbo.OutgoingMessages
        SET SenderRole = CASE
                            WHEN SenderRole = 'DDZ' THEN 'MeteringPointAdministrator'
                            WHEN SenderRole = 'DDQ' THEN 'EnergySupplier'
                            WHEN SenderRole = 'DDM' THEN 'GridOperator'
                            WHEN SenderRole = 'DGL' THEN 'MeteringDataAdministrator'
                            WHEN SenderRole = 'MDR' THEN 'MeteredDataResponsible'
                            WHEN SenderRole = 'DDK' THEN 'BalanceResponsible'
                        END,
        ReceiverRole = CASE
                                 WHEN ReceiverRole = 'DDZ' THEN 'MeteringPointAdministrator'
                                 WHEN ReceiverRole = 'DDQ' THEN 'EnergySupplier'
                                 WHEN ReceiverRole = 'DDM' THEN 'GridOperator'
                                 WHEN ReceiverRole = 'DGL' THEN 'MeteringDataAdministrator'
                                 WHEN ReceiverRole = 'MDR' THEN 'MeteredDataResponsible'
                                 WHEN ReceiverRole = 'DDK' THEN 'BalanceResponsible'
                        END;
    UPDATE dbo.EnqueuedMessages
        SET SenderRole = CASE
                         WHEN SenderRole = 'DDZ' THEN 'MeteringPointAdministrator'
                         WHEN SenderRole = 'DDQ' THEN 'EnergySupplier'
                         WHEN SenderRole = 'DDM' THEN 'GridOperator'
                         WHEN SenderRole = 'DGL' THEN 'MeteringDataAdministrator'
                         WHEN SenderRole = 'MDR' THEN 'MeteredDataResponsible'
                         WHEN SenderRole = 'DDK' THEN 'BalanceResponsible'
        END,
        ReceiverRole = CASE
                           WHEN ReceiverRole = 'DDZ' THEN 'MeteringPointAdministrator'
                           WHEN ReceiverRole = 'DDQ' THEN 'EnergySupplier'
                           WHEN ReceiverRole = 'DDM' THEN 'GridOperator'
                           WHEN ReceiverRole = 'DGL' THEN 'MeteringDataAdministrator'
                           WHEN ReceiverRole = 'MDR' THEN 'MeteredDataResponsible'
                           WHEN ReceiverRole = 'DDK' THEN 'BalanceResponsible'
            END