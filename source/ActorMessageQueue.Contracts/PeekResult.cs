namespace Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages;

public record PeekResult(Stream? Bundle, Guid? MessageId = default);
