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

using Energinet.DataHub.EDI.Common;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;

public sealed class Bundle
{
    private readonly int _maxNumberOfMessagesInABundle;
    private int _messageCount;

#pragma warning disable

    private Bundle()
    {
    }

    internal Bundle(BundleId id, BusinessReason businessReason, DocumentType documentTypeInBundle, int maxNumberOfMessagesInABundle, Instant created)
    {
        _maxNumberOfMessagesInABundle = maxNumberOfMessagesInABundle;
        Id = id;
        BusinessReason = businessReason;
        DocumentTypeInBundle = documentTypeInBundle;
        Created = created;
    }

    internal DocumentType DocumentTypeInBundle { get; }

    internal BundleId Id { get; }

    internal BusinessReason BusinessReason { get; }

    internal bool IsClosed { get; private set; }

    public bool IsDequeued { get; private set; }

    public Instant Created { get; private set; }

    internal void Add(OutgoingMessage outgoingMessage)
    {
        if (IsClosed)
            return;
        outgoingMessage.AssignToBundle(Id);
        _messageCount++;
        CloseBundleIfFull();
    }

    internal void Dequeue()
    {
        IsDequeued = true;
    }

    private void CloseBundleIfFull()
    {
        IsClosed = _maxNumberOfMessagesInABundle == _messageCount;
    }

    public void CloseBundle()
    {
        IsClosed = true;
    }
}
