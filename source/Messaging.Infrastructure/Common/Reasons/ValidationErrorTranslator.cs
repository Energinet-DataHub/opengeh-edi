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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Common.Reasons;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;

namespace Messaging.Infrastructure.Common.Reasons;

internal class ValidationErrorTranslator : IValidationErrorTranslator
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ValidationErrorTranslator(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<ReadOnlyCollection<Reason>> TranslateAsync(IEnumerable<string> validationErrors)
    {
        return TranslateAsync(validationErrors, string.Empty);
    }

    public async Task<ReadOnlyCollection<Reason>> TranslateAsync(IEnumerable<string> validationErrors, string language)
    {
        var sql = "SELECT [Text], [Code], [ErrorCode], [Id], [Lang] FROM [b2b].[Reasons] WHERE [Lang] = @Lang";

        var result = await _connectionFactory
            .GetOpenConnection()
            .QueryAsync<Reason>(sql, new { Lang = language })
            .ConfigureAwait(false);

        var reasons = validationErrors
            .Select(error => result
                .ToList()
                .FirstOrDefault(x => x.ErrorCode.Equals(error, StringComparison.OrdinalIgnoreCase)
                                     && x.Lang.Equals(language, StringComparison.OrdinalIgnoreCase))
                             ?? new Reason(string.Empty, string.Empty, string.Empty, Guid.Empty, string.Empty))
            .ToList().AsReadOnly();

        return reasons;
    }
}
