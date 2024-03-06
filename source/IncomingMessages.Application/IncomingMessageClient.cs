﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser;
using Energinet.DataHub.EDI.IncomingMessages.Application.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageValidators;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.Process.Interfaces;
using Microsoft.Extensions.Logging;
using Serie = Energinet.DataHub.EDI.Process.Interfaces.Serie;

namespace Energinet.DataHub.EDI.IncomingMessages.Application;

public class IncomingMessageClient : IIncomingMessageClient
{
    private readonly MarketMessageParser _marketMessageParser;
    private readonly RequestAggregatedMeasureDataMessageValidator _requestAggregatedMeasureDataMarketMessageValidator;
    private readonly ResponseFactory _responseFactory;
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly ILogger<IncomingMessageClient> _logger;
    private readonly IRequestAggregatedMeasureDataReceiver _requestAggregatedMeasureDataReceiver;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public IncomingMessageClient(
        MarketMessageParser marketMessageParser,
        RequestAggregatedMeasureDataMessageValidator requestAggregatedMeasureDataMarketMessageValidator,
        ResponseFactory responseFactory,
        IArchivedMessagesClient archivedMessagesClient,
        ILogger<IncomingMessageClient> logger,
        IRequestAggregatedMeasureDataReceiver requestAggregatedMeasureDataReceiver,
        ISystemDateTimeProvider systemDateTimeProvider)
    {
        _marketMessageParser = marketMessageParser;
        _requestAggregatedMeasureDataMarketMessageValidator = requestAggregatedMeasureDataMarketMessageValidator;
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

        var incomingMarketMessageParserResult =
            await _marketMessageParser.ParseAsync(incomingMessageStream, documentFormat, documentType, cancellationToken)
                .ConfigureAwait(false);

        if (incomingMarketMessageParserResult.Errors.Count != 0 || incomingMarketMessageParserResult.IncomingMessage == null)
        {
            var res = Result.Failure(incomingMarketMessageParserResult.Errors.ToArray());
            _logger.LogInformation("Failed to parse incoming message. Errors: {Errors}", res.Errors);
            return _responseFactory.From(res, responseFormat ?? documentFormat);
        }

        await ArchiveIncomingMessageAsync(incomingMessageStream, incomingMarketMessageParserResult.IncomingMessage, cancellationToken)
            .ConfigureAwait(false);

        var validationResult =
            documentType == IncomingDocumentType.RequestWholesaleSettlement
                ? Result.Succeeded()
                : await _requestAggregatedMeasureDataMarketMessageValidator
            .ValidateAsync(incomingMarketMessageParserResult.IncomingMessage as RequestAggregatedMeasureDataMessage ?? throw new InvalidOperationException(), cancellationToken)
            .ConfigureAwait(false);

        if (!validationResult.Success)
        {
            _logger.LogInformation(
                "Failed to validate incoming message: {MessageId}. Errors: {Errors}",
                incomingMarketMessageParserResult.IncomingMessage?.MessageId,
                incomingMarketMessageParserResult.Errors);
            return _responseFactory.From(validationResult, responseFormat ?? documentFormat);
        }

        var result = documentType == IncomingDocumentType.RequestWholesaleSettlement
            ? Result.Failure()
            : await ReceiveRequestAggregatedMeasureDataMessageAsync(incomingMarketMessageParserResult, cancellationToken)
                .ConfigureAwait(false);

        if (result.Success)
        {
            return new ResponseMessage();
        }

        _logger.LogInformation(
            "Failed to save incoming message: {MessageId}. Errors: {Errors}",
            incomingMarketMessageParserResult.IncomingMessage!.MessageId,
            incomingMarketMessageParserResult.Errors);
        return _responseFactory.From(result, responseFormat ?? documentFormat);
    }

    private async Task<Result> ReceiveRequestAggregatedMeasureDataMessageAsync(IncomingMarketMessageParserResult incomingMarketMessageParserResult, CancellationToken cancellationToken)
    {
        var aggregatedMeasureDataRequestMessage = incomingMarketMessageParserResult.IncomingMessage as RequestAggregatedMeasureDataMessage ??
                                                  throw new InvalidOperationException();
        var series = aggregatedMeasureDataRequestMessage.Series
            .Select(
                serie => new Serie(
                    serie.Id,
                    serie.MarketEvaluationPointType,
                    serie.MarketEvaluationSettlementMethod,
                    serie.StartDateAndOrTimeDateTime,
                    serie.EndDateAndOrTimeDateTime,
                    serie.MeteringGridAreaDomainId,
                    serie.EnergySupplierMarketParticipantId,
                    serie.BalanceResponsiblePartyMarketParticipantId,
                    serie.SettlementSeriesVersion)).ToList().AsReadOnly();

        return await _requestAggregatedMeasureDataReceiver.ReceiveAsync(
            new RequestAggregatedMeasureDataDto(
                aggregatedMeasureDataRequestMessage.SenderNumber,
                aggregatedMeasureDataRequestMessage.SenderRoleCode,
                aggregatedMeasureDataRequestMessage.ReceiverNumber,
                aggregatedMeasureDataRequestMessage.ReceiverRoleCode,
                aggregatedMeasureDataRequestMessage.BusinessReason,
                aggregatedMeasureDataRequestMessage.MessageType,
                aggregatedMeasureDataRequestMessage.MessageId,
                aggregatedMeasureDataRequestMessage.CreatedAt,
                aggregatedMeasureDataRequestMessage.BusinessType,
                series),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task ArchiveIncomingMessageAsync(
        IIncomingMessageStream incomingMessageStream,
        IncomingMessage incomingMessage,
        CancellationToken cancellationToken)
    {
        await _archivedMessagesClient.CreateAsync(
            new ArchivedMessage(
                incomingMessage.MessageId,
                IncomingDocumentType.RequestAggregatedMeasureData.Name,
                incomingMessage.SenderNumber,
                incomingMessage.ReceiverNumber,
                _systemDateTimeProvider.Now(),
                incomingMessage.BusinessReason,
                ArchivedMessageType.IncomingMessage,
                incomingMessageStream),
            cancellationToken).ConfigureAwait(false);
    }
}
