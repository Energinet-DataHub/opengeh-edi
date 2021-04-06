using System;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Commands;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public static class MessageTypeFactory
    {
        public static Type GetType(string type)
        {
            switch (type)
            {
                case nameof(ChangeSupplier):
                    return typeof(ChangeSupplier);
                case nameof(NotifyCurrentSupplier):
                    return typeof(NotifyCurrentSupplier);
                case nameof(NotifyGridOperator):
                    return typeof(NotifyGridOperator);
                case nameof(SendConfirmationMessage):
                    return typeof(SendConfirmationMessage);
                case nameof(SendConsumerDetails):
                    return typeof(SendConsumerDetails);
                case nameof(SendMeteringPointDetails):
                    return typeof(SendMeteringPointDetails);
                default: throw new ArgumentException();
            }
        }
    }
}
