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
        reasons.AddRange(reasonTranslations.Select(translation => new Reason(translation.Text, translation.Code, string.Empty, Guid.Empty, ReasonLanguage.DK)));
        return reasons.AsReadOnly();
    }

    public async Task<ReadOnlyCollection<Reason>> TranslateAsync(IEnumerable<string> validationErrors, string? language)
    {
        return string.IsNullOrEmpty(language) ?
            FilterReasons(validationErrors.ToList(), await GetReasonsAsync().ConfigureAwait(false) as List<Reason> ?? throw new InvalidOperationException())
            : FilterReasonsByLang(validationErrors.ToList(), language, await GetReasonsByLanguageAsync(language).ConfigureAwait(false) as List<Reason>);
    }

    private static ReadOnlyCollection<Reason> FilterReasons(IEnumerable<string> validationErrors, IReadOnlyCollection<Reason> reasons)
    {
        var result = validationErrors
            .SelectMany(error => reasons.Where(reason => reason.ErrorCode.Equals(error, StringComparison.OrdinalIgnoreCase)))
            .ToList()
            .AsReadOnly();

        return MergeTexts(result);
    }

    private static ReadOnlyCollection<Reason> FilterReasonsByLang(IEnumerable<string> validationErrors, string? language, List<Reason>? reasons)
    {
        var result = validationErrors
            .Select(error => (reasons ?? throw new InvalidOperationException()).FirstOrDefault(reason =>
                                 reason.ErrorCode.Equals(error, StringComparison.OrdinalIgnoreCase)
                                 && reason.Lang.EnumToString().Equals(language, StringComparison.OrdinalIgnoreCase))
                             ?? new Reason(string.Empty, string.Empty, string.Empty, Guid.Empty, ReasonLanguage.Unknown))
            .ToList().AsReadOnly();
        return result;
    }

    private static ReadOnlyCollection<Reason> MergeTexts(ReadOnlyCollection<Reason> reasons)
    {
        var result = new List<Reason>();
        foreach (var firstReason in reasons)
        {
            foreach (var secondReason in reasons)
            {
                if (!result.Exists(x => x.ErrorCode.Equals(firstReason.ErrorCode, StringComparison.OrdinalIgnoreCase)) &&
                    firstReason.ErrorCode.Equals(secondReason.ErrorCode, StringComparison.OrdinalIgnoreCase) &&
                    firstReason.Code.Equals(secondReason.Code, StringComparison.OrdinalIgnoreCase) &&
                    !firstReason.Lang.Equals(secondReason.Lang))
                {
                    var merged = firstReason.Text + "/" + secondReason.Text;
                    firstReason.Text = merged;
                    firstReason.Lang = ReasonLanguage.Mixed;
                    result.Add(firstReason);
                }
            }
        }

        return new ReadOnlyCollection<Reason>(result);
    }

    private static IEnumerable<Reason> GetUnregisteredReasons(List<string> errorCodes, List<ReasonTranslation> reasonTranslations)
    {
        var unregisteredCodes = errorCodes.Except(reasonTranslations.Select(x => x.ErrorCode));
        return unregisteredCodes.Select(errorCode => new Reason($"No code and text found for {errorCode}", "999", errorCode, Guid.Empty, ReasonLanguage.Unknown)).ToList();
    }

    private async Task<IEnumerable<Reason>> GetReasonsAsync()
    {
        const string sql = "SELECT [Text], [Code], [ErrorCode], [Id], [LanguageCode] FROM [b2b].[ReasonTranslations]";

        var result = await _connectionFactory
            .GetOpenConnection()
            .QueryAsync<Reason>(sql)
            .ConfigureAwait(false);
        return result;
    }

    private async Task<IEnumerable<Reason>> GetReasonsByLanguageAsync(string language)
    {
        const string sql = "SELECT [Text], [Code], [ErrorCode], [Id], [LanguageCode] FROM [b2b].[ReasonTranslations] WHERE [LanguageCode] = @Lang";

        var result = await _connectionFactory
            .GetOpenConnection()
            .QueryAsync<Reason>(sql, new { Lang = language })
            .ConfigureAwait(false);
        return result;
    }

    private async Task<List<ReasonTranslation>> GetTranslationsAsync(IEnumerable<string> errorCodes)
    {
        const string sql = "SELECT [Text], [Code], [ErrorCode], [Id], [LanguageCode] FROM [b2b].[ReasonTranslations] WHERE ErrorCode IN @ErrorCodes AND LanguageCode = 'dk'";

        var result = await _connectionFactory
            .GetOpenConnection()
            .QueryAsync<ReasonTranslation>(sql, new { ErrorCodes = errorCodes })
            .ConfigureAwait(false);

        return result.ToList();
    }

    private record ReasonTranslation(string Text, string Code, string ErrorCode, Guid Id, string LanguageCode);
}
