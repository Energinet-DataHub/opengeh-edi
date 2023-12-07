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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using IncomingMessages.Infrastructure;
using IncomingMessages.Infrastructure.Messages;
using IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using IncomingMessages.Infrastructure.Response;
using Microsoft.Data.SqlClient;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.Application;

public class IncomingMessageParser : IIncomingMessageParser
{
    private readonly MarketMessageParser _marketMessageParser;
    private readonly IncomingRequestAggregatedMeasuredDataSender _incomingRequestAggregatedMeasuredDataSender;
    private readonly RequestAggregatedMeasureDataValidator _aggregatedMeasureDataMarketMessageValidator;
    private readonly ResponseFactory _responseFactory;
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

    public IncomingMessageParser(
        MarketMessageParser marketMessageParser,
        IncomingRequestAggregatedMeasuredDataSender incomingRequestAggregatedMeasuredDataSender,
        RequestAggregatedMeasureDataValidator aggregatedMeasureDataMarketMessageValidator,
        ResponseFactory responseFactory,
        IArchivedMessagesClient archivedMessagesClient,
        IDatabaseConnectionFactory databaseConnectionFactory)
    {
        _marketMessageParser = marketMessageParser;
        _incomingRequestAggregatedMeasuredDataSender = incomingRequestAggregatedMeasuredDataSender;
        _aggregatedMeasureDataMarketMessageValidator = aggregatedMeasureDataMarketMessageValidator;
        _responseFactory = responseFactory;
        _archivedMessagesClient = archivedMessagesClient;
        _databaseConnectionFactory = databaseConnectionFactory;
    }

    public async Task<ResponseMessage> ParseAsync(
        Stream message,
        DocumentFormat documentFormat,
        IncomingDocumentType documentType,
        CancellationToken cancellationToken,
        DocumentFormat responseFormat = null!)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var requestAggregatedMeasureDataMarketMessageParserResult =
            await _marketMessageParser.ParseAsync(message, documentFormat, documentType, cancellationToken).ConfigureAwait(false);

        if (requestAggregatedMeasureDataMarketMessageParserResult.Errors.Any())
        {
            var res = Result.Failure(requestAggregatedMeasureDataMarketMessageParserResult.Errors.ToArray());
            return _responseFactory.From(res, responseFormat ?? documentFormat);
        }

        EnsureStreamIsRewound(message);
        await _archivedMessagesClient.CreateAsync(
            new ArchivedMessage(
                Guid.NewGuid().ToString(),
                requestAggregatedMeasureDataMarketMessageParserResult.Dto!.MessageId,
                IncomingDocumentType.RequestAggregatedMeasureData.Name,
                requestAggregatedMeasureDataMarketMessageParserResult.Dto!.SenderNumber,
                requestAggregatedMeasureDataMarketMessageParserResult.Dto!.ReceiverNumber,
                SystemClock.Instance.GetCurrentInstant(),
                requestAggregatedMeasureDataMarketMessageParserResult.Dto!.BusinessReason,
                message),
            cancellationToken).ConfigureAwait(false);

        using var connection =
            (SqlConnection)await _databaseConnectionFactory.GetConnectionAndOpenAsync(cancellationToken)
                .ConfigureAwait(false);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var result = await _aggregatedMeasureDataMarketMessageValidator
            .ValidateAsync(requestAggregatedMeasureDataMarketMessageParserResult.Dto!, cancellationToken)
            .ConfigureAwait(false);

        if (result.Success)
        {
            await _incomingRequestAggregatedMeasuredDataSender.SendAsync(
                    requestAggregatedMeasureDataMarketMessageParserResult.Dto!,
                    cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new ResponseMessage();
        }

        return _responseFactory.From(result, responseFormat ?? documentFormat);
    }

    private static void EnsureStreamIsRewound(Stream message)
    {
        message.Seek(0, SeekOrigin.Begin);
    }
}
