using MediatR;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class InternalCommand
    {
        public string? Data { get; set; }

        public int Id { get; set; }

        public string? Type { get;  set; }
    }
}
