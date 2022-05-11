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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Common.Reasons;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Application.Transactions;

namespace Messaging.Infrastructure.Common.Reasons;

internal class ValidationErrorTranslator : IValidationErrorTranslator
{
#pragma warning disable CA1822 // Cannot be made static
    public Task<ReadOnlyCollection<Reason>> TranslateAsync(IEnumerable<ValidationError> validationErrors)
#pragma warning restore CA1822
    {
        var reasons = validationErrors
            .Select(validationError => new Reason(validationError.Message, validationError.Code))
            .ToList();
        return Task.FromResult(reasons.AsReadOnly());
    }
}
