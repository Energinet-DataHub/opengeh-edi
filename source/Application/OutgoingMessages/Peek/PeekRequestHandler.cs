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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.Commands.Commands;
using Domain.Actors;
using Domain.ArchivedMessages;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.Peek;
using MediatR;
using NodaTime;

namespace Application.OutgoingMessages.Peek;

public class PeekRequestHandler : IRequestHandler<PeekRequest, PeekResult>
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly DocumentFactory _documentFactory;
    private readonly IEnqueuedMessages _enqueuedMessages;
    private readonly IBundledMessages _bundledMessages;
    private readonly IArchivedMessageRepository _messageArchive;

    public PeekRequestHandler(
        ISystemDateTimeProvider systemDateTimeProvider,
        DocumentFactory documentFactory,
        IEnqueuedMessages enqueuedMessages,
        IBundledMessages bundledMessages,
        IArchivedMessageRepository messageArchive)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _documentFactory = documentFactory;
        _enqueuedMessages = enqueuedMessages;
        _bundledMessages = bundledMessages;
        _messageArchive = messageArchive;
    }

    public async Task<PeekResult> Handle(PeekRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bundledMessage = await _bundledMessages.GetAsync(request.MessageCategory, request.ActorNumber, cancellationToken).ConfigureAwait(false);
        if (bundledMessage is not null)
        {
            return new PeekResult(bundledMessage.GeneratedDocument, bundledMessage.Id.Value);
        }

        var messageRecords = await _enqueuedMessages.GetByAsync(request.ActorNumber, request.MessageCategory, cancellationToken)
            .ConfigureAwait(false);

        if (messageRecords is null)
        {
            return new PeekResult(null);
        }

        var timestamp = _systemDateTimeProvider.Now();
        bundledMessage = await CreateBundledMessageAsync(messageRecords, request.DesiredDocumentFormat, timestamp).ConfigureAwait(false);
        await _bundledMessages.AddAsync(bundledMessage, cancellationToken).ConfigureAwait(false);

        _messageArchive.Add(new ArchivedMessage(
            bundledMessage.Id.Value,
            messageRecords.DocumentType,
            ActorNumber.Create(messageRecords.SenderNumber),
            ActorNumber.Create(messageRecords.ReceiverNumber),
            timestamp,
            ProcessType.From(messageRecords.ProcessType)));

        return new PeekResult(bundledMessage.GeneratedDocument, bundledMessage.Id.Value);
    }

    private async Task<BundledMessage> CreateBundledMessageAsync(MessageRecords messageRecords, DocumentFormat desiredDocumentFormat, Instant timestamp)
    {
        var id = BundledMessageId.New();
        var document = await _documentFactory.CreateFromAsync(id, messageRecords, desiredDocumentFormat, timestamp)
            .ConfigureAwait(false);
        return BundledMessage.CreateFrom(id, messageRecords, document);
    }
}

public record PeekRequest(ActorNumber ActorNumber, MessageCategory MessageCategory, DocumentFormat DesiredDocumentFormat) : ICommand<PeekResult>;

public record PeekResult(Stream? Bundle, Guid? MessageId = default);
