using System.Threading;
using System.Threading.Tasks;

namespace CimMessageAdapter.Messages;

/// <summary>
/// Validation for Message Type
/// </summary>
public interface IMessageTypeValidator
{
    /// <summary>
    /// Validates  Message Type
    /// </summary>
    /// <param name="messageType"></param>
    /// <param name="cancellationToken"></param>
    public Task<Result> ValidateAsync(string messageType, CancellationToken cancellationToken);
}

public class DefaultMessageTypeValidator : IMessageTypeValidator
{
    public Task<Result> ValidateAsync(string messageType, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Succeeded());
    }
}
