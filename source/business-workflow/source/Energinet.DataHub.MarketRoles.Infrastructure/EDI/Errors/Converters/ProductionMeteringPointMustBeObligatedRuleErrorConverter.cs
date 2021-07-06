using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class ProductionMeteringPointMustBeObligatedRuleErrorConverter : ErrorConverter<ProductionMeteringPointMustBeObligatedRuleError>
    {
        protected override Error Convert(ProductionMeteringPointMustBeObligatedRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
