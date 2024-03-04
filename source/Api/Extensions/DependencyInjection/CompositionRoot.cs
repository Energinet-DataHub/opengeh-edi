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
using System.Net.Http;
using Dapper;
using Dapper.NodaTime;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Common.Serialization;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Api.Extensions.DependencyInjection;

public class CompositionRoot
{
    private readonly IServiceCollection _services;

    private CompositionRoot(IServiceCollection services)
    {
        _services = services;
        services.AddSingleton<HttpClient>()
            .AddSingleton<ISerializer, Serializer>();

        AddMediatR();
        services.AddLogging();
        AddAuthenticatedActor();
        AddDapper(services);
    }

    public static CompositionRoot Initialize(IServiceCollection services)
    {
        return new CompositionRoot(services);
    }

    public CompositionRoot AddSystemClock(ISystemDateTimeProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _services.AddScoped(sp => provider);
        return this;
    }

    private static IServiceCollection AddDapper(IServiceCollection services)
    {
        ConfigureDapper();

        return services;
    }

    private static void ConfigureDapper()
    {
        SqlMapper.AddTypeHandler(InstantHandler.Default);
    }

    private void AddMediatR()
    {
        var configuration = new MediatRServiceConfiguration();
        ServiceRegistrar.AddRequiredServices(_services, configuration);
    }

    private void AddAuthenticatedActor()
    {
        _services.AddScoped<AuthenticatedActor>();
    }
}
