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

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.Process.Interfaces;
using IncomingMessages.Infrastructure;
using IncomingMessages.Infrastructure.Messages;
using IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using IncomingMessages.Infrastructure.Response;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.Application;

public class IncomingRequestAggregatedMeasuredData : IIncomingRequestAggregatedMeasuredData
{
    private readonly RequestAggregatedMeasureDataMarketMessageParser _requestAggregatedMeasureDataMarketMessageParser;
    private readonly IncomingRequestAggregatedMeasuredDataSender _incomingRequestAggregatedMeasuredDataSender;
    private readonly RequestAggregatedMeasureDataValidator _aggregatedMeasureDataMarketMessageValidator;
    private readonly ResponseFactory _responseFactory;
    private readonly IArchivedMessagesClient _archivedMessagesClient;

    public IncomingRequestAggregatedMeasuredData(
        RequestAggregatedMeasureDataMarketMessageParser requestAggregatedMeasureDataMarketMessageParser,
        IncomingRequestAggregatedMeasuredDataSender incomingRequestAggregatedMeasuredDataSender,
        RequestAggregatedMeasureDataValidator aggregatedMeasureDataMarketMessageValidator,
        ResponseFactory responseFactory,
        IArchivedMessagesClient archivedMessagesClient)
    {
        _requestAggregatedMeasureDataMarketMessageParser = requestAggregatedMeasureDataMarketMessageParser;
        _incomingRequestAggregatedMeasuredDataSender = incomingRequestAggregatedMeasuredDataSender;
        _aggregatedMeasureDataMarketMessageValidator = aggregatedMeasureDataMarketMessageValidator;
        _responseFactory = responseFactory;
        _archivedMessagesClient = archivedMessagesClient;
    }

    public async Task<ResponseMessage> ParseAsync(Stream message, DocumentFormat documentFormat, CancellationToken cancellationToken, DocumentFormat responseFormat = null!)
    {
        var requestAggregatedMeasureDataMarketMessageParserResult = await _requestAggregatedMeasureDataMarketMessageParser.ParseAsync(message, documentFormat, cancellationToken).ConfigureAwait(false);

        await SaveArchivedMessageAsync(requestAggregatedMeasureDataMarketMessageParserResult.MarketMessage!, message, cancellationToken).ConfigureAwait(false);

        if (requestAggregatedMeasureDataMarketMessageParserResult.Errors.Any())
        {
            var res = Result.Failure(requestAggregatedMeasureDataMarketMessageParserResult.Errors.ToArray());
            return _responseFactory.From(res, responseFormat ?? documentFormat);
        }

        // Note that the current implementation could save the messageId and transactionId, then fail to send the service bus message.
        var result = await _aggregatedMeasureDataMarketMessageValidator
            .ValidateAsync(requestAggregatedMeasureDataMarketMessageParserResult.MarketMessage!, cancellationToken)
            .ConfigureAwait(false);

        if (result.Success)
        {
            await _incomingRequestAggregatedMeasuredDataSender.SendAsync(
                    requestAggregatedMeasureDataMarketMessageParserResult.MarketMessage!,
                    cancellationToken)
                .ConfigureAwait(false);

            return new ResponseMessage();
        }

        return _responseFactory.From(result, responseFormat ?? documentFormat);
    }

    private async Task SaveArchivedMessageAsync(RequestAggregatedMeasureDataMarketMessage marketMessage, Stream document, CancellationToken hostCancellationToken)
    {
       await _archivedMessagesClient.CreateAsync(
            new ArchivedMessage(
            Guid.NewGuid().ToString(),
            marketMessage.MessageId,
            IncomingDocumentType.RequestAggregatedMeasureData.Name,
            marketMessage.SenderNumber,
            marketMessage.ReceiverNumber,
            SystemClock.Instance.GetCurrentInstant(),
            marketMessage.BusinessReason,
            document),
            hostCancellationToken).ConfigureAwait(false);
    }
}
