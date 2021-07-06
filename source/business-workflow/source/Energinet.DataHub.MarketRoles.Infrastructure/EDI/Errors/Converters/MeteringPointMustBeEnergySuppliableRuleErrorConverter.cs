using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class MeteringPointMustBeEnergySuppliableRuleErrorConverter : ErrorConverter<MeteringPointMustBeEnergySuppliableRuleError>
    {
        protected override Error Convert(MeteringPointMustBeEnergySuppliableRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
