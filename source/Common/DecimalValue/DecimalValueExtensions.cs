namespace Energinet.DataHub.EDI.Common.DecimalValue;

public static class DecimalValueExtensions
{
    private static decimal? Parse(this DecimalValue input)
    {
        const decimal nanoFactor = 1_000_000_000;
        return input.Units + (input.Nanos / nanoFactor);
    }
}
