CREATE UNIQUE INDEX UQ_OutgoingMessages_ExternalId_ReceiverNumber_ReceiverRole_PeriodStartedAt ON [dbo].[OutgoingMessages] (ExternalId, ReceiverNumber, ReceiverRole, PeriodStartedAt);
