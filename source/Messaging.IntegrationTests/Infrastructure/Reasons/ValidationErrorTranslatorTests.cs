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
        Assert.Equal("Mixed", Enum.GetName(typeof(ReasonLanguage), reasons[0].Lang));
        Assert.Equal("Kundenavn er påkrævet/Customer name is required", reasons[0].Text);

        Assert.NotEmpty(reasons);
        Assert.Equal("D64", reasons[1].Code);
        Assert.Equal("Mixed", Enum.GetName(typeof(ReasonLanguage), reasons[1].Lang));
        Assert.Equal("Målepunkts ID er påkrævet/Metering point ID is required", reasons[1].Text);
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
        Assert.Equal("Mixed", Enum.GetName(typeof(ReasonLanguage), reasons.FirstOrDefault()?.Lang ?? ReasonLanguage.Unknown));
        Assert.Equal("Kundenavn er påkrævet/Customer name is required", reasons.FirstOrDefault()?.Text);
    }

    [Fact]
    public async Task Translator_can_translate_validation_error_to_reason_by_language()
    {
        var validationErrors = new List<string>()
        {
            "ConsumerNameIsRequired",
        };

        var reasons = await _validationErrorTranslator.TranslateAsync(validationErrors, "en").ConfigureAwait(false);

        Assert.NotEmpty(reasons);
        Assert.Equal("D64", reasons.FirstOrDefault()?.Code);
        Assert.Equal("EN", Enum.GetName(typeof(ReasonLanguage), reasons.FirstOrDefault()?.Lang ?? ReasonLanguage.Unknown));
    }
}
