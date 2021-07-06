using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class MoveOutRegisteredOnSameDateIsNotAllowedRuleErrorConverter : ErrorConverter<MoveOutRegisteredOnSameDateIsNotAllowedRuleError>
    {
        protected override Error Convert(MoveOutRegisteredOnSameDateIsNotAllowedRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
