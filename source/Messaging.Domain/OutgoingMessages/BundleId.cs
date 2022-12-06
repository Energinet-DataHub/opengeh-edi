using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;

namespace Messaging.Domain.OutgoingMessages;

public class BundleId : ValueObject
{
    private BundleId(MessageCategory messageCategory, ActorNumber receiverNumber, MarketRole receiverRole)
    {
        MessageCategory = messageCategory;
        ReceiverNumber = receiverNumber;
        ReceiverRole = receiverRole;
    }

    public MessageCategory MessageCategory { get; }

    public ActorNumber ReceiverNumber { get; }

    public MarketRole ReceiverRole { get; }

    public static BundleId Create(MessageCategory messageCategory, ActorNumber actorNumber, MarketRole marketRole)
    {
        return new BundleId(messageCategory, actorNumber, marketRole);
    }
}
