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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Queueing.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;

/// <summary>
/// Writes outgoing messages
/// </summary>
public interface IDocumentWriter
{
    /// <summary>
    /// Determine if specified format can be handled by message writer
    /// </summary>
    /// <param name="format"></param>
    bool HandlesFormat(DocumentFormat format);

    /// <summary>
    /// Determine if specified message type can be handles by the writer
    /// </summary>
    /// <param name="documentType"></param>
    bool HandlesType(DocumentType documentType);

    /// <summary>
    /// Writes the message
    /// </summary>
    /// <param name="header"></param>
    /// <param name="marketActivityRecords"></param>
    Task<MarketDocumentStream> WriteAsync(OutgoingMessageHeader header, IReadOnlyCollection<string> marketActivityRecords);
}
