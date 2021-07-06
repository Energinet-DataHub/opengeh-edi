using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class MustHaveEnergySupplierAssociatedRuleErrorConverter : ErrorConverter<MustHaveEnergySupplierAssociatedRuleError>
    {
        protected override Error Convert(MustHaveEnergySupplierAssociatedRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
