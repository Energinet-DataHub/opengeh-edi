using System;
using System.Threading.Tasks;
using Messaging.Application.Transactions;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Transactions.MoveIn;
using Processing.Infrastructure.EDI.MoveIn;

namespace Messaging.IntegrationTests.TestDoubles;

public class MoveInRequestHandlerStub : IMoveInRequestAdapter
{
    #pragma warning disable // Disable mark as static warning
    public Task<BusinessRequestResult> InvokeAsync(MoveInRequest request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(request.ConsumerName))
        {
            return Task.FromResult(BusinessRequestResult.Failure(new ValidationError("999", "somemessage")));
        }

        return Task.FromResult(BusinessRequestResult.Succeeded());
    }
    #pragma warning restore
}
