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

namespace Energinet.DataHub.EDI.Tests.DocumentValidation;

/// <summary>
/// Validates a document
/// </summary>
public interface IValidator
{
    /// <summary>
    /// Specifies the format handled
    /// </summary>
    DocumentFormat HandledFormat { get; }

    /// <summary>
    /// Validates a document
    /// </summary>
    /// <param name="document"></param>
    /// <param name="type"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <returns><see cref="ValidationResult"/></returns>
    Task<ValidationResult> ValidateAsync(
        Stream document, DocumentType type, string version, CancellationToken cancellationToken);
}
