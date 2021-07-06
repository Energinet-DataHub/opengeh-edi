using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRuleErrorConverter : ErrorConverter<ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRuleError>
    {
        protected override Error Convert(ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
