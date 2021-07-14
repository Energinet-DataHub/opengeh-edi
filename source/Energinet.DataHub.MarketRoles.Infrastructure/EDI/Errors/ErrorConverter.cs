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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors
{
    #pragma warning disable SA1402 // These ErrorConverter types "overloaded" by type parameter are tightly coupled and it seems logical to have them in the same file.
    public abstract class ErrorConverter
    {
        public abstract ErrorMessage Convert(ValidationError validationError);

        protected static ErrorMessage Default()
        {
            return new(string.Empty, string.Empty);
        }
    }

    public abstract class ErrorConverter<TError> : ErrorConverter
        where TError : ValidationError
    {
        public override ErrorMessage Convert(ValidationError validationError)
        {
            return validationError is TError specificError
                ? Convert(specificError)
                : Default();
        }

        protected abstract ErrorMessage Convert([NotNull] TError validationError);
    }
    #pragma warning restore SA1402
}
