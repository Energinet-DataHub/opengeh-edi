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

using Energinet.DataHub.Core.App.Common.Users;
using Energinet.DataHub.EDI.B2CWebApi.Security;

namespace Energinet.DataHub.EDI.B2CWebApi.Middleware;

public class FrontendUserLogScopeMiddleware(UserContext<FrontendUser> userContext, ILogger<FrontendUserLogScopeMiddleware> logger) : IMiddleware
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
            }
        }
        else
        {
            using (_logger.BeginScope(GetNoUserLogScopeProperties()))
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }

    private List<KeyValuePair<string, object>> GetLogScopeProperties(FrontendUser user)
    {
        return
        [
            new("FrontendUser.Scope", Guid.NewGuid().ToString()),
            new("UserId", user.UserId.ToString()),
            new("UserRole", user.Role),
            new("ActorId", user.ActorId.ToString()),
            new("IsFas", user.IsFas),
            new("Azp", user.Azp),
            new("ActorNumber", user.ActorNumber),
        ];
    }

    private List<KeyValuePair<string, object>> GetNoUserLogScopeProperties()
    {
        return
        [
            new("FrontendUser.Scope", Guid.NewGuid().ToString()),
            new("UserId", "<no user>"),
        ];
    }
}
