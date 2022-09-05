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

namespace Messaging.Domain.OutgoingMessages;

/// <summary>
/// Writes CIM documents
/// </summary>
public interface IDocumentWriter
{
    /// <summary>
    /// Determine if specificed CIM format can be handled by document writer
    /// </summary>
    /// <param name="format"></param>
    bool HandlesDocumentFormat(CimFormat format);

    /// <summary>
    /// Determine if specified document type can be handles by the writer
    /// </summary>
    /// <param name="documentType"></param>
    bool HandlesDocumentType(DocumentType documentType);

    /// <summary>
    /// Writes the document
    /// </summary>
    /// <param name="header"></param>
    /// <param name="marketActivityRecords"></param>
    Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords);
}
