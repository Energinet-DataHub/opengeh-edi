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
using Application.OutgoingMessages;
using Application.OutgoingMessages.Peek;
using Application.OutgoingMessages.Queueing;
using Domain.OutgoingMessages.Queueing;
using Infrastructure.OutgoingMessages.Queueing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PeekResult = Application.OutgoingMessages.Peek.PeekResult;

namespace Infrastructure.OutgoingMessages.Peek;

internal static class PeekConfiguration
{
    internal static void Configure(
        IServiceCollection services,
        IBundleConfiguration bundleConfiguration,
        Func<IServiceProvider, IBundledMessages>? bundleStoreBuilder)
    {
        services.AddScoped<MessageEnqueuer>();
        services.AddScoped<IActorMessageQueueRepository, ActorMessageQueueRepository>();
        services.AddTransient<MessagePeeker>();
        services.AddTransient<IRequestHandler<PeekRequest, PeekResult>, PeekRequestHandler>();
        services.AddTransient<IRequestHandler<PeekCommand, Domain.OutgoingMessages.Queueing.PeekResult>, PeekHandler>();
        services.AddScoped<IEnqueuedMessages, EnqueuedMessages>();
        services.AddScoped(_ => bundleConfiguration);

        if (bundleStoreBuilder is null)
        {
            services.AddScoped<IBundledMessages, BundledMessages>();
        }
        else
        {
            services.AddScoped(bundleStoreBuilder);
        }
    }
}
