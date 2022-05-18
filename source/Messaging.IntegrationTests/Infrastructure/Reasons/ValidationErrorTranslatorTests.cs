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
using Messaging.Application.Common.Reasons;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Reasons;

public class ValidationErrorTranslatorTests : TestBase
{
    private readonly IValidationErrorTranslator _validationErrorTranslator;

    public ValidationErrorTranslatorTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _validationErrorTranslator = GetService<IValidationErrorTranslator>();
    }

    [Fact]
    public async Task Translator_can_translate_multiple_validation_errors_to_reason()
    {
        var validationErrors = new List<string>()
        {
            "ConsumerNameIsRequired",
            "AccountingPointIdentifierIsRequired",
        };

        var reasons = await _validationErrorTranslator.TranslateAsync(validationErrors).ConfigureAwait(false);

        Assert.NotEmpty(reasons);
        Assert.Equal("D64", reasons[0].Code);
        Assert.Equal("Kundenavn er påkrævet", reasons[0].Text);

        Assert.NotEmpty(reasons);
        Assert.Equal("D64", reasons[1].Code);
        Assert.Equal("Målepunkts ID er påkrævet", reasons[1].Text);
    }

    [Fact]
    public async Task Translator_can_translate_validation_error_to_reason()
    {
        var validationErrors = new List<string>()
        {
            "ConsumerNameIsRequired",
        };

        var reasons = await _validationErrorTranslator.TranslateAsync(validationErrors).ConfigureAwait(false);

        Assert.NotEmpty(reasons);
        Assert.Equal("D64", reasons.FirstOrDefault()?.Code);
        Assert.Equal("Kundenavn er påkrævet", reasons.FirstOrDefault()?.Text);
    }

    [Fact]
    public async Task Return_default_reason_if_no_translation_is_registered_for_error_code()
    {
        var validationErrors = new List<string>() { "unknown error code" };

        var reasons = await _validationErrorTranslator.TranslateAsync(validationErrors).ConfigureAwait(false);

        Assert.Equal("999", reasons.First().Code);
    }
}
