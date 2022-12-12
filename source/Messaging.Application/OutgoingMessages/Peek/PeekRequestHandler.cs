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
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Application.OutgoingMessages.Peek;

public class PeekRequestHandler : IRequestHandler<PeekRequest, PeekResult>
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly DocumentFactory _documentFactory;
    private readonly IEnqueuedMessages _enqueuedMessages;
    private readonly BundleStore _bundleStore;

    public PeekRequestHandler(ISystemDateTimeProvider systemDateTimeProvider, DocumentFactory documentFactory, IEnqueuedMessages enqueuedMessages, BundleStore bundleStore)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _documentFactory = documentFactory;
        _enqueuedMessages = enqueuedMessages;
        _bundleStore = bundleStore;
    }

    public async Task<PeekResult> Handle(PeekRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bundleId = BundleId.Create(request.MessageCategory, request.ActorNumber, request.MarketRole);
        var document = await _bundleStore
            .GetBundleOfAsync(bundleId)
            .ConfigureAwait(false);

        if (document is not null) return new PeekResult(document);

        if (!await _bundleStore.TryRegisterBundleAsync(bundleId).ConfigureAwait(false)) return new PeekResult(null);

        var messages = (await _enqueuedMessages.GetByAsync(request.ActorNumber, request.MarketRole, request.MessageCategory)
            .ConfigureAwait(false))
            .ToList();

        if (messages.Count == 0)
        {
            await _bundleStore.UnregisterBundleAsync(bundleId).ConfigureAwait(false);
            return new PeekResult(null);
        }

        var bundle = CreateBundleFrom(messages.ToList());
        var cimMessage = bundle.CreateMessage();
        document = await _documentFactory.CreateFromAsync(cimMessage, CimFormat.Xml).ConfigureAwait(false);
        await _bundleStore.SetBundleForAsync(bundleId, document, bundle.MessageId, bundle.GetMessageIdsIncluded()).ConfigureAwait(false);
        return new PeekResult(document, bundle.MessageId);
    }

    private Bundle CreateBundleFrom(IReadOnlyList<EnqueuedMessage> messages)
    {
        var bundle = new Bundle(_systemDateTimeProvider.Now());
        foreach (var outgoingMessage in messages)
        {
            bundle.Add(outgoingMessage);
        }

        return bundle;
    }
}

public record PeekRequest(ActorNumber ActorNumber, MessageCategory MessageCategory, MarketRole MarketRole) : ICommand<PeekResult>;

public record PeekResult(Stream? Bundle, Guid? MessageId = default);
