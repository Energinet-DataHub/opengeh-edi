// ReSharper disable once CheckNamespace -- Must match the csharp_namespace in the DecimalValue.proto file
namespace Energinet.DataHub.Edi.Responses;

public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000;

    public static DecimalValue FromDecimal(decimal d)
    {
        var units = decimal.ToInt64(d);
        var nanos = decimal.ToInt32((d - units) * NanoFactor);
        return new DecimalValue
        {
            Units = units,
            Nanos = nanos,
        };
    }

    public decimal ToDecimal() => Units + (Nanos / NanoFactor);
}
