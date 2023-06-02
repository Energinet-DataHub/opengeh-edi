using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CimMessageAdapter.Errors;

namespace CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class ProcessTypeValidator : IProcessTypeValidator
{
    private static readonly IReadOnlyCollection<string> _whiteList = new[] { "D03", "D04", "D05", "D09", "D32" };

    public async Task<Result> ValidateAsync(string processType, CancellationToken cancellationToken)
    {
        return await Task.FromResult(!_whiteList.Contains(processType) ?
            Result.Failure(new UnknownProcessType(processType)) : Result.Succeeded()).ConfigureAwait(false);
    }
}
