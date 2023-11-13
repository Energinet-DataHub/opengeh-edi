// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using IncomingMessages.Infrastructure;
using IncomingMessages.Infrastructure.Messages;
using IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using IncomingMessages.Infrastructure.Response;

namespace Energinet.DataHub.EDI.IncomingMessages.Application;

public class IncomingRequestAggregatedMeasuredData : IIncomingRequestAggregatedMeasuredData
{
    private readonly IRequestAggregatedMeasureDataMarketMessageParser _requestAggregatedMeasureDataMarketMessageParser;
    private readonly IncomingRequestAggregatedMeasuredDataSender _incomingRequestAggregatedMeasuredDataSender;
    private readonly ISerializer _serializer;
    private readonly RequestAggregatedMeasureDataMarketMessageValidator _aggregatedMeasureDataMarketMessageValidator;
    private readonly ResponseFactory _responseFactory;

    public IncomingRequestAggregatedMeasuredData(
        IRequestAggregatedMeasureDataMarketMessageParser requestAggregatedMeasureDataMarketMessageParser,
        IncomingRequestAggregatedMeasuredDataSender incomingRequestAggregatedMeasuredDataSender,
        ISerializer serializer,
        RequestAggregatedMeasureDataMarketMessageValidator aggregatedMeasureDataMarketMessageValidator,
        ResponseFactory responseFactory)
    {
        _requestAggregatedMeasureDataMarketMessageParser = requestAggregatedMeasureDataMarketMessageParser;
        _incomingRequestAggregatedMeasuredDataSender = incomingRequestAggregatedMeasuredDataSender;
        _serializer = serializer;
        _aggregatedMeasureDataMarketMessageValidator = aggregatedMeasureDataMarketMessageValidator;
        _responseFactory = responseFactory;
    }

    public async Task<ResponseMessage> ParseAsync(Stream message, DocumentFormat documentFormat, CancellationToken cancellationToken)
    {
        var requestAggregatedMeasureDataMarketMessageParserResult = await _requestAggregatedMeasureDataMarketMessageParser.ParseAsync(message, documentFormat, cancellationToken).ConfigureAwait(false);
        // save archived message

        if (requestAggregatedMeasureDataMarketMessageParserResult.Errors.Any())
        {
            var res = Result.Failure(requestAggregatedMeasureDataMarketMessageParserResult.Errors.ToArray());
            return _responseFactory.From(res, documentFormat);
        }

        var validate = await _aggregatedMeasureDataMarketMessageValidator
            .ValidateAsync(requestAggregatedMeasureDataMarketMessageParserResult.MarketMessage!, cancellationToken)
            .ConfigureAwait(false);

        var serviceBusMessage =
            new ServiceBusMessage(
                _serializer.Serialize(requestAggregatedMeasureDataMarketMessageParserResult.MarketMessage))
            {
                Subject = requestAggregatedMeasureDataMarketMessageParserResult.MarketMessage!.ToString(), //TODO: LRN subject pattern`?
            };

        await _incomingRequestAggregatedMeasuredDataSender.SendAsync(serviceBusMessage, cancellationToken)
            .ConfigureAwait(false);

        return new ResponseMessage();
    }
}
