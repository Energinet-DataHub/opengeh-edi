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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Application.OutgoingMessages.Peek;

public class PeekRequestHandler : IRequestHandler<PeekRequest, PeekResult>
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly DocumentFactory _documentFactory;
    private readonly IOutgoingMessageStore _outgoingMessageStore;

    public PeekRequestHandler(ISystemDateTimeProvider systemDateTimeProvider, DocumentFactory documentFactory, IOutgoingMessageStore outgoingMessageStore)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _documentFactory = documentFactory;
        _outgoingMessageStore = outgoingMessageStore;
    }

    public async Task<PeekResult> Handle(PeekRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.MessageCategory == MessageCategory.MasterData)
        {
            var message = _outgoingMessageStore
                .GetUnpublished()
                .Where(message => message.DocumentType == DocumentType.RejectRequestChangeOfSupplier)
                .ToList();

            var bundle = CreateBundleFrom(message);
            var cimMessage = bundle.CreateMessage();
            var document = await _documentFactory.CreateFromAsync(cimMessage, CimFormat.Xml).ConfigureAwait(false);

            return new PeekResult(document);
        }

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

public record PeekRequest(MessageCategory MessageCategory) : ICommand<PeekResult>;

public record PeekResult(Stream? Bundle);
