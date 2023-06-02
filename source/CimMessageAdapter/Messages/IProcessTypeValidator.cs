using System.Threading;
using System.Threading.Tasks;

namespace CimMessageAdapter.Messages;

/// <summary>
/// Validation for Process Type
/// </summary>
public interface IProcessTypeValidator
{
    /// <summary>
    /// Validates Process Type
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
