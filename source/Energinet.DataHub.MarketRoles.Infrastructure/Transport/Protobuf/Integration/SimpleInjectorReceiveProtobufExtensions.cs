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
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf.Integration
{
    public static class SimpleInjectorReceiveProtobufExtensions
    {
        private static readonly Type _mapperType = typeof(ProtobufInboundMapper<>);

        /// <summary>
        /// Configure the container to receive protobuf
        /// </summary>
        /// <param name="container">container</param>
        /// <param name="configuration">Configuration of the parser</param>
        /// <param name="additionalAssemblies">Scan additional assemblies for mappers</param>
        /// <typeparam name="TProtoContract">Protobuf contract DTO</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="container"/>, <paramref name="configuration"/> or the optional <paramref name="additionalAssemblies"/> is <c>null</c></exception>
        public static void ReceiveProtobuf<TProtoContract>(
            this Container container,
            Action<OneOfConfiguration<TProtoContract>> configuration,
            params Assembly[] additionalAssemblies)
            where TProtoContract : class, IMessage
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var assemblies = additionalAssemblies.ToList();
            assemblies.Add(typeof(TProtoContract).Assembly);

            var mapperTypes = ScanForMappers(container, assemblies);

            var config = new OneOfConfiguration<TProtoContract>();
            configuration.Invoke(config);
            container.Register<MessageExtractor>(Lifestyle.Scoped);
            container.Register<MessageDeserializer, ProtobufMessageDeserializer>(Lifestyle.Scoped);
            container.Register<ProtobufInboundMapperFactory>(Lifestyle.Scoped);
            container.Register(() => config.GetParser(), Lifestyle.Scoped);
            container.Register(_mapperType, mapperTypes, Lifestyle.Scoped);
        }

        private static IEnumerable<Type> ScanForMappers(Container container, IEnumerable<Assembly> assemblies)
        {
            return container.GetTypesToRegister(_mapperType, assemblies, new TypesToRegisterOptions
            {
                IncludeGenericTypeDefinitions = true,
                IncludeComposites = false,
            });
        }
    }
}
