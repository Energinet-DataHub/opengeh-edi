-- Index used when finding ReceivedIntegrationEvents to be removed in retention job

CREATE NONCLUSTERED INDEX [IX_ReceivedIntegrationEvents_OccurredOn] 
    ON [dbo].[ReceivedIntegrationEvents] ([OccurredOn])