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

using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.Core.App.Common.Users;
using Energinet.DataHub.EDI.B2CWebApi.Security;

namespace Energinet.DataHub.EDI.B2CWebApi.Middleware;

public class FrontendUserLogScopeMiddleware(UserContext<FrontendUser> userContext, ILogger logger) : IMiddleware
{
    private readonly UserContext<FrontendUser> _userContext = userContext;
    private readonly ILogger _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        FrontendUser? user = null;
        try
        {
            user = _userContext.CurrentUser;
        }
        catch (InvalidOperationException)
        {
            // No user found, don't add it to logging scope
        }

        if (user != null)
        {
            using (_logger.BeginScope(GetLogScopeProperties(user)))
            {
                await next(context).ConfigureAwait(false);
                _logger.LogInformation("Closing FrontendUser.Scope");
            }
        }
        else
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private IDictionary<string, object> GetLogScopeProperties(FrontendUser user)
    {
        return new Dictionary<string, object>
        {
            { "FrontendUser.Scope", Guid.NewGuid().ToString() },
            { "UserId", user.UserId.ToString() },
            { "UserRole", user.Role },
            { "ActorId", user.ActorId.ToString() },
            { "IsFas", user.IsFas },
            { "Azp", user.Azp },
            { "ActorNumber", user.ActorNumber },
        };
    }
}
