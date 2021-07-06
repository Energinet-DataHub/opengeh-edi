using Energinet.DataHub.MarketRoles.Application.Common.Validation;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class StartOfSupplyMustBeValidRuleErrorConverter : ErrorConverter<StartOfSupplyMustBeValidRuleError>
    {
        protected override Error Convert(StartOfSupplyMustBeValidRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
