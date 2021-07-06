using Energinet.DataHub.MarketRoles.Application.Common.Validation;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class TransactionMustBeValidRuleErrorConverter : ErrorConverter<TransactionMustBeValidRuleError>
    {
        protected override Error Convert(TransactionMustBeValidRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
