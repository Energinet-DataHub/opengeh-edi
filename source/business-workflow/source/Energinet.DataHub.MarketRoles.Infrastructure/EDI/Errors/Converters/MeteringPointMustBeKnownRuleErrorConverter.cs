using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Validation;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class MeteringPointMustBeKnownRuleErrorConverter : ErrorConverter<MeteringPointMustBeKnownRuleError>
    {
        protected override Error Convert(MeteringPointMustBeKnownRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
