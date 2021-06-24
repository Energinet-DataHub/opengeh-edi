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
using System.Reflection;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector
{
    public static class RegisterProtobuf
    {
        public static IServiceCollection RegisterProtoContracts<TProtoContract>(this IServiceCollection services)
            where TProtoContract : class, IMessage
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddScoped<ProtobufOutboundMapperFactory>();
            services.TryAddScoped<MessageSerializer, ProtobufMessageSerializer>();

            foreach (var serviceDescriptor in ScanForMappers(typeof(TProtoContract).Assembly))
            {
                services.Add(serviceDescriptor);
            }

            return services;
        }

        private static IEnumerable<ServiceDescriptor> ScanForMappers(Assembly targetAssembly)
        {
            var targetType = typeof(ProtobufOutboundMapper<>);
            foreach (var type in targetAssembly.GetTypes())
            {
                if (type.BaseType == null) continue;
                if (type.BaseType.IsGenericType == false) continue;
                if (type.BaseType.GetGenericTypeDefinition() == targetType == false) continue;

                var genericTypeParameter = type.BaseType.GenericTypeArguments[0];

                yield return new ServiceDescriptor(targetType.MakeGenericType(genericTypeParameter), type, ServiceLifetime.Scoped);
            }
        }
    }
}
