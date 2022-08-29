using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Messaging.Infrastructure.Transactions.MoveIn;

/// <summary>
/// Request dispatcher interface
/// </summary>
public interface IRequestDispatcher
{
    /// <summary>
    /// Async method for sending servicebus messages
    /// </summary>
    /// <param name="message"></param>
    /// <returns><see cref="ServiceBusMessage"/></returns>
    Task SendAsync(ServiceBusMessage message);
}
