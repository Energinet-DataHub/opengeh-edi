using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CimMessageAdapter.Errors;

namespace CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class MessageTypeValidator : IMessageTypeValidator
{
    private static readonly IReadOnlyCollection<string> _whiteList = new[] { "E74" };

    public async Task<Result> ValidateAsync(string messageType, CancellationToken cancellationToken)
    {
        return await Task.FromResult(!_whiteList.Contains(messageType) ?
            Result.Failure(new UnknownMessageType(messageType)) : Result.Succeeded()).ConfigureAwait(false);
    }
}
