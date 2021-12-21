using Energinet.DataHub.MarketRoles.Application.Common.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Integration.Notifications.MeteringPointCreated
{
    public class MeteringPointCreatedInboundMapper : ProtobufInboundMapper<NotificationContracts.MeteringPointCreated>
    {
        protected override IInboundMessage Convert(NotificationContracts.MeteringPointCreated obj)
        {
            throw new System.NotImplementedException();
        }
    }
}
