using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;

namespace Messaging.Domain.OutgoingMessages;

public class BundleId : ValueObject
{
    private BundleId(MessageCategory messageCategory, ActorNumber actorNumber, MarketRole marketRole)
    {
        MessageCategory = messageCategory;
        ActorNumber = actorNumber;
        MarketRole = marketRole;
    }

    public MessageCategory MessageCategory { get; }

    public ActorNumber ActorNumber { get; }

    public MarketRole MarketRole { get; }

    public static BundleId Create(MessageCategory messageCategory, ActorNumber actorNumber, MarketRole marketRole)
    {
        return new BundleId(messageCategory, actorNumber, marketRole);
    }
}
