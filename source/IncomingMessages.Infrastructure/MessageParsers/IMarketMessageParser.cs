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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;

/// <summary>
/// Parses market messages from a stream
/// </summary>
public interface IMarketMessageParser
{
    /// <summary>
    /// The CIM format handled
    /// </summary>
    DocumentFormat HandledFormat { get; }

    /// <summary>
    /// The document type handled by the market message parser
    /// </summary>
    IncomingDocumentType DocumentType { get; }

    /// <summary>
    /// Parse from stream
    /// </summary>
    Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream, CancellationToken cancellationToken);
}
