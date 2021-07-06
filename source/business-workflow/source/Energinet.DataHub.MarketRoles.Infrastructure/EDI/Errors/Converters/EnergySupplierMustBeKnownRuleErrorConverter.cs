using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Validation;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class EnergySupplierMustBeKnownRuleErrorConverter : ErrorConverter<EnergySupplierMustBeKnownRuleError>
    {
        protected override Error Convert(EnergySupplierMustBeKnownRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
