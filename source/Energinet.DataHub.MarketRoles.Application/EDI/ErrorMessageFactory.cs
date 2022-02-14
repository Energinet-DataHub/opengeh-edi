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
using System.Linq;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Application.EDI
{
    public record ErrorConverterRegistration(Type Error, Func<ErrorConverter> Func);

    public class ErrorMessageFactory
    {
        private readonly Dictionary<Type, Func<ErrorConverter>> _converters;

        public ErrorMessageFactory(IEnumerable<ErrorConverterRegistration> registrations)
        {
            _converters = registrations.ToDictionary(x => x.Error, x => x.Func);
        }

        public ErrorMessage GetErrorMessage(ValidationError validationError)
        {
            if (validationError == null) throw new ArgumentNullException(nameof(validationError));
            if (string.IsNullOrEmpty(validationError.Code) == false)
            {
                return new ErrorMessage(validationError.Code, validationError.Message);
            }

            return _converters[validationError.GetType()]().Convert(validationError);
        }
    }
}
