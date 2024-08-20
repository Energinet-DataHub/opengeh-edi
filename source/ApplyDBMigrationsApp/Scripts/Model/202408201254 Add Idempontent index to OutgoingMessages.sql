CREATE UNIQUE INDEX IDX_OutgoingMessage_ReceiverRole_ExternalId_PeriodStartedAt ON [dbo].[OutgoingMessages] (ExternalId, ReceiverRole, PeriodStartedAt);
