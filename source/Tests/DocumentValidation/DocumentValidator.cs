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

public class DocumentValidator
{
    private readonly IEnumerable<IValidator> _validators;

    public DocumentValidator(IEnumerable<IValidator> validators)
    {
        _validators = validators;
    }

    public async Task<ValidationResult> ValidateAsync(
        Stream message,
        DocumentFormat format,
        DocumentType document,
        CancellationToken cancellationToken,
        string version = "0.1")
    {
        var validator = ValidatorFor(format);
        return await validator.ValidateAsync(message, document, version, cancellationToken).ConfigureAwait(false);
    }

    private IValidator ValidatorFor(DocumentFormat format)
    {
        return _validators.First(validator => validator.HandledFormat == format);
    }
}
