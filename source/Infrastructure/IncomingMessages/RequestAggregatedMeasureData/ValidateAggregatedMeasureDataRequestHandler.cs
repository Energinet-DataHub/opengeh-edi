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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using CimMessageAdapter.Response;
using Domain.Actors;
using Domain.ArchivedMessages;
using Domain.Documents;
using MediatR;
using Receiver = CimMessageAdapter.Messages.RequestAggregatedMeasureData.RequestAggregatedMeasureDataReceiver;

namespace Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class ValidateAggregatedMeasureDataRequestHandler
    : IRequestHandler<ReceiveAggregatedMeasureDataRequest, Result>
{
    private readonly RequestAggregatedMeasureDataReceiver _messageReceiver;
    private readonly IArchivedMessageRepository _messageArchive;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public ValidateAggregatedMeasureDataRequestHandler(
        Receiver messageReceiver,
        IArchivedMessageRepository messageArchive,
        ISystemDateTimeProvider systemDateTimeProvider)
    {
        _messageReceiver = messageReceiver;
        _messageArchive = messageArchive;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public async Task<Result> Handle(ReceiveAggregatedMeasureDataRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var messageHeader = request.MessageResult.IncomingMarketDocument?.Header;
        ArgumentNullException.ThrowIfNull(messageHeader);

        var timestamp = _systemDateTimeProvider.Now();

        _messageArchive.Add(new ArchivedMessage(
            Guid.NewGuid().ToString(),
            messageHeader.MessageId?.Substring(0, 36),
            IncomingDocumentType.RequestAggregatedMeasureData,
            TryGetActorNumber(messageHeader.SenderId),
            TryGetActorNumber(messageHeader.ReceiverId),
            timestamp,
            messageHeader.BusinessReason,
            request.OriginalMessage));

        var result = await _messageReceiver.ReceiveAsync(request.MessageResult, cancellationToken)
            .ConfigureAwait(false);

        return result;
    }

    private static ActorNumber? TryGetActorNumber(string messageHeaderSenderId)
    {
        try
        {
            return ActorNumber.Create(messageHeaderSenderId);
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            return null;
        }
    }
}
