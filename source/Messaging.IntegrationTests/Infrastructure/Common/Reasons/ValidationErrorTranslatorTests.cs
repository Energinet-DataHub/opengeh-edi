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
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages.Common.Reasons;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Common.Reasons;

public class ValidationErrorTranslatorTests : TestBase
{
    private readonly IValidationErrorTranslator _validationErrorTranslator;

    public ValidationErrorTranslatorTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _validationErrorTranslator = GetService<IValidationErrorTranslator>();
    }

    [Fact]
    public async Task Translator_can_translate_validation_error_to_reason()
    {
        var errorCode = "SomeErrorCode";
        var code = "123";
        var text = "Some error description";
        await RegisterTranslation(errorCode, code, text).ConfigureAwait(false);
        var validationErrors = new List<string>()
        {
            errorCode,
        };

        var reasons = await _validationErrorTranslator.TranslateAsync(validationErrors).ConfigureAwait(false);

        Assert.NotEmpty(reasons);
        Assert.Equal(code, reasons.FirstOrDefault()?.Code);
    }

    [Fact]
    public async Task Return_default_reason_if_no_translation_is_registered_for_error_code()
    {
        var validationErrors = new List<string>() { "unknown error code" };

        var reasons = await _validationErrorTranslator.TranslateAsync(validationErrors).ConfigureAwait(false);

        Assert.Equal("999", reasons.First().Code);
    }

    private async Task RegisterTranslation(string errorCode, string code, string text)
    {
        var connectionFactory = GetService<IDatabaseConnectionFactory>();
        using var connection = await connectionFactory.GetConnectionAndOpenAsync().ConfigureAwait(false);
        const string insertStatement = $"INSERT INTO [b2b].[ReasonTranslations] (Id, ErrorCode, Code, Text, LanguageCode) " +
                                       $"VALUES (@Id, @ErrorCode, @Code, @Text, 'dk');";

        await connection.ExecuteAsync(insertStatement, new
        {
            Id = Guid.NewGuid().ToString(),
            ErrorCode = errorCode,
            Code = code,
            Text = text,
        }).ConfigureAwait(false);
    }
}
