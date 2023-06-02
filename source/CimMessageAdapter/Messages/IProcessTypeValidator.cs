using System.Threading;
using System.Threading.Tasks;

namespace CimMessageAdapter.Messages;

/// <summary>
/// Validation for ProcessType
/// </summary>
public interface IProcessTypeValidator
{
    /// <summary>
    /// Validates ProcessType
    /// </summary>
    /// <param name="processType"></param>
    /// <param name="cancellationToken"></param>
    public Task<Result> ValidateAsync(string processType, CancellationToken cancellationToken);
}

public class DefaultProcessTypeValidator : IProcessTypeValidator
{
    public Task<Result> ValidateAsync(string processType, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Succeeded());
    }
}
