using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class MoveInRegisteredOnSameDateIsNotAllowedRuleErrorConverter : ErrorConverter<MoveInRegisteredOnSameDateIsNotAllowedRuleError>
    {
        protected override Error Convert(MoveInRegisteredOnSameDateIsNotAllowedRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
