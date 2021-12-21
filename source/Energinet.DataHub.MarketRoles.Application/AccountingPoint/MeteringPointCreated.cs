using Energinet.DataHub.MarketRoles.Application.Common.Transport;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Application.AccountingPoint
{
    public record MeteringPointCreated(
            string MeteringPointId,
            string MeteringPointType,
            string GsrnNumber)
        : INotification, IInboundMessage;
}
