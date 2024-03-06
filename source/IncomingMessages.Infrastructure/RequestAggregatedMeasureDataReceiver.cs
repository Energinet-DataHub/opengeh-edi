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
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Exceptions;
using Energinet.DataHub.EDI.Process.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure;

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

    public async Task ReceiveAsync(
        RequestAggregatedMeasureDataDto requestAggregatedMeasureDataDto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestAggregatedMeasureDataDto);

        await AddMessageIdAndTransactionIdAsync(requestAggregatedMeasureDataDto, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await ResilientTransaction.New(_incomingMessagesContext, async () =>
                {
                    await _incomingRequestAggregatedMeasuredDataSender.SendAsync(
                            requestAggregatedMeasureDataDto,
                            cancellationToken)
                        .ConfigureAwait(false);
                })
                .SaveChangesAsync(new DbContext[] { _incomingMessagesContext, })
                .ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlException
                                           && sqlException.Message.Contains(
                                               "Violation of PRIMARY KEY constraint 'PK_TransactionRegistry_TransactionIdAndSenderId'",
                                               StringComparison.OrdinalIgnoreCase))
        {
            throw new DuplicateTransactionIdDetectedException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlException
                                           && sqlException.Message.Contains(
                                               "Violation of PRIMARY KEY constraint 'PK_MessageRegistry_MessageIdAndSenderId'",
                                               StringComparison.OrdinalIgnoreCase))
        {
            throw new DuplicateMessageIdDetectedException();
        }
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
