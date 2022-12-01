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

using MediatR;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Peek;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Infrastructure.OutgoingMessages.Peek;

internal static class PeekConfiguration
{
    internal static void Configure(IServiceCollection services, IBundleConfiguration bundleConfiguration, IBundleStore bundleStore, IBundlingState bundlingState)
    {
        services.AddTransient<IRequestHandler<PeekRequest, PeekResult>, PeekRequestHandler>();
        services.AddScoped<IEnqueuedMessages, EnqueuedMessages>();
        services.AddScoped(_ => bundleConfiguration);
        services.AddSingleton(_ => bundleStore);
        services.AddSingleton(_ => bundlingState);
    }
}
