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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Application.OutgoingMessages.Peek;

public class PeekRequestHandler : IRequestHandler<PeekRequest, PeekResult>
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly DocumentFactory _documentFactory;
    private readonly IOutgoingMessages _outgoingMessages;

    public PeekRequestHandler(ISystemDateTimeProvider systemDateTimeProvider, DocumentFactory documentFactory, IOutgoingMessages outgoingMessages)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _documentFactory = documentFactory;
        _outgoingMessages = outgoingMessages;
    }

    public async Task<PeekResult> Handle(PeekRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var nextMessage = await _outgoingMessages.GetNextAsync(request.ActorNumber, request.MessageCategory).ConfigureAwait(false);

        if (nextMessage is not null)
        {
            var documentTypeToBundle = nextMessage.DocumentType;
            var processTypeToBundle = nextMessage.ProcessType;
            var actorRoleTypeToBundle = nextMessage.ReceiverRole;
            var message = await _outgoingMessages.GetNextByAsync(documentTypeToBundle, processTypeToBundle, actorRoleTypeToBundle).ConfigureAwait(false);
            while (message != null)
            {
                message = await _outgoingMessages.GetNextByAsync(documentTypeToBundle, processTypeToBundle, actorRoleTypeToBundle).ConfigureAwait(false);
            }
        }

        // if (request.MessageCategory == MessageCategory.MasterData)
        // {
        //     var message = _outgoingMessageQueue
        //         .GetUnpublished()
        //         .Where(message => message.DocumentType == DocumentType.ConfirmRequestChangeOfSupplier)
        //         .ToList();
        //
        //     var bundle = CreateBundleFrom(message);
        //     var cimMessage = bundle.CreateMessage();
        //     var document = await _documentFactory.CreateFromAsync(cimMessage, CimFormat.Xml).ConfigureAwait(false);
        //
        //     return new PeekResult(document);
        // }
        return new PeekResult(null);
    }

    private Bundle CreateBundleFrom(IReadOnlyList<OutgoingMessage> messages)
    {
        var bundle = new Bundle(_systemDateTimeProvider.Now());
        foreach (var outgoingMessage in messages)
        {
            bundle.Add(outgoingMessage);
        }

        return bundle;
    }
}

public record PeekRequest(ActorNumber ActorNumber, MessageCategory MessageCategory) : ICommand<PeekResult>;

public record PeekResult(Stream? Bundle);
