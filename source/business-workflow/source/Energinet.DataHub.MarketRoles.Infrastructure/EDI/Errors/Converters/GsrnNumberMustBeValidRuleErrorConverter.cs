using Energinet.DataHub.MarketRoles.Application.Common.Validation;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class GsrnNumberMustBeValidRuleErrorConverter : ErrorConverter<GsrnNumberMustBeValidRuleError>
    {
        protected override Error Convert(GsrnNumberMustBeValidRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
