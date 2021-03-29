using System;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class MessageTypeFactory
    {
        public Type GetType(string type)
        {
            switch (type)
            {
                case nameof(RequestChangeOfSupplier):
                    return typeof(RequestChangeOfSupplier);
                case nameof(Master)
            }
        }
    }
}
