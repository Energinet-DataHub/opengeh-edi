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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.DateTime;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.Application;

public class IncomingMessageClient : IIncomingMessageClient
{
    private readonly MarketMessageParser _marketMessageParser;
    private readonly RequestAggregatedMeasureDataValidator _aggregatedMeasureDataMarketMessageValidator;
    private readonly ResponseFactory _responseFactory;
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly ILogger<IncomingMessageClient> _logger;
    private readonly IRequestAggregatedMeasureDataReceiver _requestAggregatedMeasureDataReceiver;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public IncomingMessageClient(
        MarketMessageParser marketMessageParser,
        RequestAggregatedMeasureDataValidator aggregatedMeasureDataMarketMessageValidator,
        ResponseFactory responseFactory,
        IArchivedMessagesClient archivedMessagesClient,
        ILogger<IncomingMessageClient> logger,
        IRequestAggregatedMeasureDataReceiver requestAggregatedMeasureDataReceiver,
        ISystemDateTimeProvider systemDateTimeProvider)
    {
        _marketMessageParser = marketMessageParser;
        _aggregatedMeasureDataMarketMessageValidator = aggregatedMeasureDataMarketMessageValidator;
        _responseFactory = responseFactory;
        _archivedMessagesClient = archivedMessagesClient;
        _logger = logger;
        _requestAggregatedMeasureDataReceiver = requestAggregatedMeasureDataReceiver;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public async Task<ResponseMessage> RegisterAndSendAsync(
        IIncomingMessageStream incomingMessageStream,
        DocumentFormat documentFormat,
        IncomingDocumentType documentType,
        CancellationToken cancellationToken,
        DocumentFormat responseFormat = null!)
    {
        ArgumentNullException.ThrowIfNull(incomingMessageStream);

        var requestAggregatedMeasureDataMarketMessageParserResult =
            await _marketMessageParser.ParseAsync(incomingMessageStream, documentFormat, documentType, cancellationToken)
                .ConfigureAwait(false);

        if (requestAggregatedMeasureDataMarketMessageParserResult.Errors.Count != 0 && requestAggregatedMeasureDataMarketMessageParserResult.Dto == null)
        {
            var res = Result.Failure(requestAggregatedMeasureDataMarketMessageParserResult.Errors.ToArray());
            _logger.LogInformation("Failed to parse incoming message. Errors: {Errors}", res.Errors);
            return _responseFactory.From(res, responseFormat ?? documentFormat);
        }

        await ArchiveIncomingMessageAsync(incomingMessageStream, requestAggregatedMeasureDataMarketMessageParserResult, cancellationToken)
            .ConfigureAwait(false);

        var validationResult = await _aggregatedMeasureDataMarketMessageValidator
            .ValidateAsync(requestAggregatedMeasureDataMarketMessageParserResult.Dto!, cancellationToken)
            .ConfigureAwait(false);

        if (!validationResult.Success)
        {
            _logger.LogInformation(
                "Failed to validate incoming message: {MessageId}. Errors: {Errors}",
                requestAggregatedMeasureDataMarketMessageParserResult.Dto?.MessageId,
                requestAggregatedMeasureDataMarketMessageParserResult.Errors);
            return _responseFactory.From(validationResult, responseFormat ?? documentFormat);
        }

        var result = await _requestAggregatedMeasureDataReceiver.ReceiveAsync(
            requestAggregatedMeasureDataMarketMessageParserResult.Dto!,
            cancellationToken).ConfigureAwait(false);

        if (result.Success)
        {
            return new ResponseMessage();
        }

        _logger.LogInformation(
            "Failed to save incoming message: {MessageId}. Errors: {Errors}",
            requestAggregatedMeasureDataMarketMessageParserResult.Dto!.MessageId,
            requestAggregatedMeasureDataMarketMessageParserResult.Errors);
        return _responseFactory.From(result, responseFormat ?? documentFormat);
    }

    private async Task ArchiveIncomingMessageAsync(
        IIncomingMessageStream incomingMessageStream,
        RequestAggregatedMeasureDataMarketMessageParserResult requestAggregatedMeasureDataMarketMessageParserResult,
        CancellationToken cancellationToken)
    {
        await _archivedMessagesClient.CreateAsync(
            new ArchivedMessage(
                requestAggregatedMeasureDataMarketMessageParserResult.Dto!.MessageId,
                IncomingDocumentType.RequestAggregatedMeasureData.Name,
                requestAggregatedMeasureDataMarketMessageParserResult.Dto!.SenderNumber,
                requestAggregatedMeasureDataMarketMessageParserResult.Dto!.ReceiverNumber,
                _systemDateTimeProvider.Now(),
                requestAggregatedMeasureDataMarketMessageParserResult.Dto!.BusinessReason,
                ArchivedMessageType.IncomingMessage,
                incomingMessageStream),
            cancellationToken).ConfigureAwait(false);
    }
}
