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

using Energinet.DataHub.EDI.B2BApi.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Configuration.Middleware.Authentication;

public class MarketActorAuthenticatorMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger _logger;

    // DO NOT inject scoped services in the middleware constructor.
    // DO use scoped services in middleware by retrieving them from 'FunctionContext.InstanceServices'
    // DO NOT store scoped services in fields or properties of the middleware object. See https://github.com/Azure/azure-functions-dotnet-worker/issues/1327#issuecomment-1434408603
    public MarketActorAuthenticatorMiddleware(
        ILogger<MarketActorAuthenticatorMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var authenticatedActor = context.GetService<AuthenticatedActor>();
        var authenticationMethods = context.GetServices<IAuthenticationMethod>();

        if (context.EndpointIsOmittedFromAuth())
        {
            _logger.LogInformation("Functions is omitted from auth, skipping authentication");
            await next(context);
            return;
        }

        var httpRequestData = await context.GetHttpRequestDataAsync()
            ?? throw new ArgumentException("No HTTP request data was available, even though the function was not omitted from auth");

        var authenticationMethod = authenticationMethods.Single(a => a.ShouldHandle(httpRequestData));

        var authenticated = await authenticationMethod.AuthenticateAsync(httpRequestData, context.CancellationToken);

        if (!authenticated)
        {
            _logger.LogError("Could not authenticate market actor identity by using {AuthenticationMethod}", authenticationMethod.GetType().Name);
            context.RespondWithUnauthorized(httpRequestData);
            return;
        }

        var serializer = context.GetService<ISerializer>();
        WriteAuthenticatedIdentityToLog(authenticatedActor.CurrentActorIdentity, serializer);
        await next(context);
    }

    private void WriteAuthenticatedIdentityToLog(ActorIdentity? actorIdentity, ISerializer serializer)
    {
        _logger.LogInformation("Successfully authenticated market actor identity.");
        _logger.LogInformation(serializer.Serialize(actorIdentity));
    }
}
