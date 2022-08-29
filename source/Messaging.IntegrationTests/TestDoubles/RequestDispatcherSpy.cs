using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Messaging.Infrastructure.Transactions.MoveIn;

namespace Messaging.IntegrationTests.TestDoubles;

public class RequestDispatcherSpy : IRequestDispatcher
{
    private readonly Dictionary<string, ServiceBusMessage> _dispatchedRequests = new();

    public ServiceBusMessage? GetRequest(string correlationId)
    {
        _dispatchedRequests.TryGetValue(correlationId, out var message);
        return message;
    }

    public Task SendAsync(ServiceBusMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        _dispatchedRequests.Add(message.MessageId, message);
        return Task.CompletedTask;
    }
}
