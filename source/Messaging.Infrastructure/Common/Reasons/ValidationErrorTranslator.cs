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

    public async Task<ReadOnlyCollection<Reason>> TranslateAsync(IEnumerable<string> validationErrors)
    {
        var reasons = new List<Reason>();
        var errorCodes = validationErrors.ToList();
        var reasonTranslations = await GetTranslationsAsync(errorCodes).ConfigureAwait(false);

        reasons.AddRange(GetUnregisteredReasons(errorCodes, reasonTranslations));
        reasons.AddRange(reasonTranslations.Select(translation => new Reason(translation.Text, translation.Code)));
        return reasons.AsReadOnly();
    }

    private static IEnumerable<Reason> GetUnregisteredReasons(List<string> errorCodes, List<ReasonTranslation> reasonTranslations)
    {
        var unregisteredCodes = errorCodes.Except(reasonTranslations.Select(x => x.ErrorCode));
        return unregisteredCodes.Select(errorCode => new Reason($"No code and text found for {errorCode}", "999")).ToList();
    }

    private async Task<List<ReasonTranslation>> GetTranslationsAsync(IEnumerable<string> errorCodes)
    {
        const string sql = "SELECT [Text], [Code], [ErrorCode] FROM [b2b].[ReasonTranslations] WHERE ErrorCode IN @ErrorCodes AND LanguageCode = 'dk'";

        var result = await _connectionFactory
            .GetOpenConnection()
            .QueryAsync<ReasonTranslation>(sql, new { ErrorCodes = errorCodes })
            .ConfigureAwait(false);

        return result.ToList();
    }

    private record ReasonTranslation(string Text, string Code, string ErrorCode);
}
