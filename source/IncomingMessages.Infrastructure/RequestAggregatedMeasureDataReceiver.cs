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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using IncomingMessages.Infrastructure.Configuration.DataAccess;
using IncomingMessages.Infrastructure.Messages;
using IncomingMessages.Infrastructure.ValidationErrors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IncomingMessages.Infrastructure;

public class RequestAggregatedMeasureDataReceiver : IRequestAggregatedMeasureDataReceiver
{
    private readonly IncomingRequestAggregatedMeasuredDataSender _incomingRequestAggregatedMeasuredDataSender;
    private readonly IncomingMessagesContext _incomingMessagesContext;
    private readonly IMessageIdRepository _messageIdRepository;
    private readonly ITransactionIdRepository _transactionIdRepository;

    public RequestAggregatedMeasureDataReceiver(
        IncomingRequestAggregatedMeasuredDataSender incomingRequestAggregatedMeasuredDataSender,
        IncomingMessagesContext incomingMessagesContext,
        IMessageIdRepository messageIdRepository,
        ITransactionIdRepository transactionIdRepository)
    {
        _incomingRequestAggregatedMeasuredDataSender = incomingRequestAggregatedMeasuredDataSender;
        _incomingMessagesContext = incomingMessagesContext;
        _messageIdRepository = messageIdRepository;
        _transactionIdRepository = transactionIdRepository;
    }

    public async Task<Result> ReceiveAsync(
        RequestAggregatedMeasureDataDto requestAggregatedMeasureDataDto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestAggregatedMeasureDataDto);

        await AddMessageIdAndTransactionIdAsync(requestAggregatedMeasureDataDto, cancellationToken)
            .ConfigureAwait(false);

        var result = await SaveMessageIdAndTransactionIdAsync(
                    requestAggregatedMeasureDataDto,
                    cancellationToken)
                .ConfigureAwait(false);

        if (result.Success)
        {
            await _incomingRequestAggregatedMeasuredDataSender.SendAsync(
                    requestAggregatedMeasureDataDto,
                    cancellationToken)
                .ConfigureAwait(false);

            await ResilientTransaction.New(_incomingMessagesContext)
                .SaveChangesAsync(new DbContext[] { _incomingMessagesContext, })
                .ConfigureAwait(false);
        }

        return result;
    }

    private async Task<Result> SaveMessageIdAndTransactionIdAsync(
        RequestAggregatedMeasureDataDto requestAggregatedMeasureDataDto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _incomingMessagesContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlException
                                           && sqlException.Message.Contains(
                                               "Violation of PRIMARY KEY constraint 'PK_TransactionRegistry_TransactionIdAndSenderId'",
                                               StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new DuplicateTransactionIdDetected());
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlException
                                           && sqlException.Message.Contains(
                                               "Violation of PRIMARY KEY constraint 'PK_MessageRegistry_MessageIdAndSenderId'",
                                               StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new DuplicateMessageIdDetected(requestAggregatedMeasureDataDto.MessageId));
        }

        return Result.Succeeded();
    }

    private async Task AddMessageIdAndTransactionIdAsync(
        RequestAggregatedMeasureDataDto requestAggregatedMeasureDataDto,
        CancellationToken cancellationToken)
    {
        await _transactionIdRepository.AddAsync(
            requestAggregatedMeasureDataDto.SenderNumber,
            requestAggregatedMeasureDataDto.Series.Select(x => x.Id).ToList(),
            cancellationToken).ConfigureAwait(false);
        await _messageIdRepository.AddAsync(
                requestAggregatedMeasureDataDto.SenderNumber,
                requestAggregatedMeasureDataDto.MessageId,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
