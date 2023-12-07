UPDATE [dbo].[ActorCertificate]
    SET Thumbprint = NEWID(),
        SequenceNumber = 0;
    