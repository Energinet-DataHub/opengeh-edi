using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;

namespace Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages;

public record PeekCommand(ActorNumber ActorNumber, MessageCategory MessageCategory, MarketRole ActorRole, DocumentFormat DocumentFormat) : ICommand<PeekResult>;
