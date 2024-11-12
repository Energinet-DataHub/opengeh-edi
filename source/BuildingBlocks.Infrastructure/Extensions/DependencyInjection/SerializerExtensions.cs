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

using System.Text.Encodings.Web;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Extensions.DependencyInjection;

public static class SerializerExtensions
{
    public static IServiceCollection AddSerializer(this IServiceCollection services)
    {
        services.AddSingleton<ISerializer, Serializer>(
            sp => new Serializer(sp.GetRequiredService<JavaScriptEncoder>()));

        return services;
    }
}
