using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors.Converters
{
    public class EffectuationDateCannotBeInThePastRuleErrorConverter : ErrorConverter<EffectuationDateCannotBeInThePastRuleError>
    {
        protected override Error Convert(EffectuationDateCannotBeInThePastRuleError error)
        {
            return new("TODO", $"Description");
        }
    }
}
