using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class CannotBeInStateOfClosedDownRuleErrorConverter : ErrorConverter<CannotBeInStateOfClosedDownRuleError>
    {
        protected override Error Convert(CannotBeInStateOfClosedDownRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
