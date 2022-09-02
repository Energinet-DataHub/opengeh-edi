using System;
using System.Threading.Tasks;
using Energinet.DataHub.EnergySupplying.RequestResponse.Requests;
using Messaging.Application.MasterData;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.MasterDataReceivers;

public class CustomerMasterDataResponseListener
{
    private readonly ILogger<CustomerMasterDataResponseListener> _logger;
    private readonly ISerializer _serializer;
    private readonly CommandSchedulerFacade _commandSchedulerFacade;

    public CustomerMasterDataResponseListener(ISerializer serializer, CommandSchedulerFacade commandSchedulerFacade, ILogger<CustomerMasterDataResponseListener> logger)
    {
        _serializer = serializer;
        _commandSchedulerFacade = commandSchedulerFacade;
        _logger = logger;
    }

    [Function("CustomerMasterDataResponseListener")]
    public async Task RunAsync([ServiceBusTrigger("CUSTOMER_DATA_RESPONSE_QUEUE_NAME", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS_LISTENER")] string data, FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var metaData = GetMetaData(context);
        var masterDataContent = GetMasterDataContent(CustomerMasterDataRequestResponse.Parser.ParseJson(data));

        var forwardedCustomerMasterData = new ForwardCustomerMasterData(
            metaData.TransactionId ?? throw new InvalidOperationException("Service bus metadata property TransactionId is missing"),
            masterDataContent);

        await _commandSchedulerFacade.EnqueueAsync(forwardedCustomerMasterData).ConfigureAwait(false);
        _logger.LogInformation($"Master data response received: {data}");
    }

    private static CustomerMasterDataContent GetMasterDataContent(CustomerMasterDataRequestResponse masterdata)
    {
        throw new NotImplementedException();
    }

    private MasterDataResponseMetadata GetMetaData(FunctionContext context)
    {
        context.BindingContext.BindingData.TryGetValue("UserProperties", out var metadata);

        if (metadata is null)
        {
            throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
        }

        return _serializer.Deserialize<MasterDataResponseMetadata>(metadata.ToString() ?? throw new InvalidOperationException());
    }
}
