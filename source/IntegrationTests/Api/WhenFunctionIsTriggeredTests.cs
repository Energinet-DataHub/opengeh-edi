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

using System.Threading.Tasks;
using Energinet.DataHub.Core.App.FunctionApp;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Authentication;
using Energinet.DataHub.EDI.IntegrationTests.Api.Mocks;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Api;

[IntegrationTest]
public class WhenFunctionIsTriggeredTests : TestBase
{
    private readonly NextSpy _nextSpy;
    private readonly FunctionExecutionDelegate _next;
    private readonly FunctionContextBuilder _functionContextBuilder;

    public WhenFunctionIsTriggeredTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        AuthenticatedActor.SetAuthenticatedActor(null);
        _nextSpy = new NextSpy();
        _next = _nextSpy.Next;
        _functionContextBuilder = new FunctionContextBuilder(ServiceProvider);
    }

    [Theory]
    [InlineData(TriggerType.TimerTrigger)]
    [InlineData(TriggerType.ServiceBusTrigger)]
    public async Task When_calling_authentication_middleware_without_being_a_http_trigger_then_next_is_called(TriggerType triggerType)
    {
        // Arrange
        var functionContext = _functionContextBuilder
            .WithTriggeredBy(triggerType)
            .Build();

        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.True(_nextSpy.NextWasCalled);
    }

    [Theory]
    [InlineData("HealthCheck")]
    [InlineData("RenderSwaggerUI")]
    [InlineData("RenderSwaggerDocument")]
    public async Task When_calling_authentication_middleware_with_specific_function_name_then_next_is_called(string functionName)
    {
        // Arrange
        var functionContext = _functionContextBuilder
            .TriggeredByHttp()
            .WithFunctionName(functionName)
            .Build();

        var sut = CreateMarketActorAuthenticatorMiddleware();

        // Act
        await sut.Invoke(functionContext, _next);

        // Assert
        Assert.True(_nextSpy.NextWasCalled);
    }

    private static MarketActorAuthenticatorMiddleware CreateMarketActorAuthenticatorMiddleware()
    {
        var stubLogger = new Logger<MarketActorAuthenticatorMiddleware>(NullLoggerFactory.Instance);

        var sut = new MarketActorAuthenticatorMiddleware(stubLogger);
        return sut;
    }
}
