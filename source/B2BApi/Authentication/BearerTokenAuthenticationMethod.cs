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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.B2BApi.Common;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Authentication;

public class BearerTokenAuthenticationMethod : IAuthenticationMethod
{
    private readonly ILogger<BearerTokenAuthenticationMethod> _logger;
    private readonly JwtTokenParser _jwtTokenParser;
    private readonly IMarketActorAuthenticator _marketActorAuthenticator;

    public BearerTokenAuthenticationMethod(ILogger<BearerTokenAuthenticationMethod> logger, JwtTokenParser jwtTokenParser, IMarketActorAuthenticator marketActorAuthenticator)
    {
        _logger = logger;
        _jwtTokenParser = jwtTokenParser;
        _marketActorAuthenticator = marketActorAuthenticator;
    }

    public bool ShouldHandle(HttpRequestData httpRequestData)
    {
        ArgumentNullException.ThrowIfNull(httpRequestData);

        var contentType = httpRequestData.Headers.TryGetContentType();
        return contentType != "application/ebix";
    }

    public Task<bool> AuthenticateAsync(HttpRequestData httpRequestData, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpRequestData);

        var result = _jwtTokenParser.ParseFrom(httpRequestData.Headers);
        if (result.Success == false)
        {
            LogParseResult(result);
            return Task.FromResult(false);
        }

        if (result.ClaimsPrincipal == null)
        {
            throw new ArgumentException("Claims principal was null after successful parsing of JWT token");
        }

        return _marketActorAuthenticator.AuthenticateAsync(result.ClaimsPrincipal, cancellationToken);
    }

    private void LogParseResult(Result result)
    {
        var message = new StringBuilder();
        message.AppendLine("Failed to parse claims principal from JWT:");
        message.AppendLine(result.Error?.Message);
        message.AppendLine("Token from HTTP request header:");
        message.AppendLine(result.Token);
        _logger.LogError(message.ToString());
    }
}
