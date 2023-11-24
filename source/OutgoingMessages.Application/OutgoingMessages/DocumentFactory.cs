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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Exceptions;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;

public class DocumentFactory
{
    private readonly IReadOnlyCollection<IDocumentWriter> _documentWriters;

    public DocumentFactory(IEnumerable<IDocumentWriter> documentWriters)
    {
        _documentWriters = documentWriters.ToList();
    }

    public Task<Stream> CreateFromAsync(OutgoingMessageBundle bundle, DocumentFormat documentFormat, Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(documentFormat);
        ArgumentNullException.ThrowIfNull(bundle);

        var documentWriter =
            _documentWriters.FirstOrDefault(writer =>
            {
                return writer.HandlesType(bundle.DocumentType) &&
                       writer.HandlesFormat(documentFormat);
            });

        if (documentWriter is null)
        {
            throw new OutgoingMessageException($"Could not handle document type {bundle.DocumentType} in format {documentFormat}");
        }

        return documentWriter.WriteAsync(
            new OutgoingMessageHeader(
                bundle.BusinessReason,
                bundle.SenderId.Value,
                bundle.SenderRole.Name,
                bundle.Receiver.Number.Value,
                bundle.Receiver.ActorRole.Name,
                bundle.AssignedBundleId.Id.ToString(),
                timestamp),
            bundle.OutgoingMessages.Select(message => message.MessageRecord).ToList());
    }
}
