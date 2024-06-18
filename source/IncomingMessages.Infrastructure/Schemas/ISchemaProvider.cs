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

using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas;

/// <summary>
/// Provides XML schemas for CIM messages
/// </summary>
public interface ISchemaProvider
{
    /// <summary>
    /// Get schema for specific business process and version
    /// </summary>
    /// <param name="businessProcessType"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="XmlSchema"/></returns>
    Task<T?> GetSchemaAsync<T>(string businessProcessType, string version, CancellationToken cancellationToken);
}

/// <summary>
/// Provider for CIM XML schemas
/// </summary>
/// <typeparam name="TSchema">Schema object type</typeparam>
public interface ISchemaProvider<TSchema>
{
    /// <summary>
    /// Get the schema for the specified document type and version
    /// </summary>
    /// <param name="type"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    Task<TSchema?> GetAsync(DocumentType type, string version, CancellationToken cancellationToken);
}
