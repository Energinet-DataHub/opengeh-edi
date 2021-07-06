using Energinet.DataHub.MarketRoles.Domain.Consumers.Rules;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class CprNumberFormatRuleErrorConverter : ErrorConverter<CprNumberFormatRuleError>
    {
        protected override Error Convert(CprNumberFormatRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
